using System;
using System.Collections.Generic;
using System.Collections.Map;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Schedulers;

public class TopologicalScheduler<Key, Value> : IBuildSystem<Key, Value>
    where Key : IEquatable<Key>
    where Value : notnull
{
    private readonly ILogger<TopologicalScheduler<Key, Value>> _logger;

    private readonly IRebuilder<Key, Value> _rebuilder;
    private readonly ITotalMap<Key, Key[]> _dependencies;
    private readonly IPartialMap<Key, IBuildTask<Key, Value>> _tasks;
    private readonly IStore<Key, Value> _store;

    public TopologicalScheduler(
        ILoggerFactory loggerFactory,
        IRebuilder<Key, Value> rebuilder,
        ITotalMap<Key, Key[]> dependencies,
        IPartialMap<Key, IBuildTask<Key, Value>> tasks,
        IStore<Key, Value> store)
    {
        _logger = loggerFactory.CreateLogger<TopologicalScheduler<Key, Value>>();

        _rebuilder = rebuilder;
        _dependencies = dependencies;
        _tasks = tasks;
        _store = store;
    }

    public async Task<Value> Build(Key target)
    {
        _logger.LogInformation("Building {Key}", target);
        var stopWatch = Stopwatch.StartNew();

        var dependencyGraph = Reachable(target);

        var sorter = new TopoloicalSorter<Key>();
        var order = sorter.Sort(dependencyGraph);

        var fetcher = new StoreBuildSystem<Key, Value>(_store);

        Value? result = default;
        foreach (var dependency in order)
        {
            try
            {
                var task = _tasks.Get(dependency);

                result = await _rebuilder.Rebuild(dependency, task, fetcher);
            }
            catch (MissingKeyException<Key>)
            {
                // Doesn't make sense to ignore?
                // But Haskell code ignores it
                continue;
            }
        }

        stopWatch.Stop();

        _logger.LogInformation("Took {Duration}ms to build {Key}", stopWatch.ElapsedMilliseconds, target);

        if (result != null)
        {
            return result;
        }
        else
        {
            throw new Exception("Key could not be built");
        }
    }

    private DirectedGraph<Key> Reachable(Key key)
    {
        var graph = new DirectedGraph<Key>();

        var visited = new HashSet<Key>();

        var dependencies = new List<Key>();
        dependencies.Add(key);

        while (dependencies.Any())
        {
            var currentKey = dependencies.First();
            dependencies.Remove(currentKey);

            if (visited.Contains(currentKey))
            {
                // Already computed dependencies
                continue;
            }

            visited.Add(currentKey);

            try
            {
                var task = _tasks.Get(currentKey);

                foreach (var dependency in _dependencies.Get(currentKey))
                {
                    graph.Add(dependency, currentKey);

                    dependencies.Add(dependency);
                }
            }
            catch (MissingKeyException<Key>)
            {
                continue;
            }
        }

        return graph;
    }
}

public class DirectedGraph<Node> where Node : IEquatable<Node>
{
    public ICollection<Node> Vertices { get { return _vertices; } }

    private HashSet<Node> _vertices = new();
    private Dictionary<Node, List<Node>> _forward = new();
    private Dictionary<Node, List<Node>> _backward = new();

    public void Add(Node from, Node to)
    {
        _vertices.Add(from);
        _vertices.Add(to);

        if (_forward.TryGetValue(from, out var forwards))
        {
            forwards.Add(to);
        }
        else
        {
            _forward[from] = [to];
        }

        if (_backward.TryGetValue(to, out var backwards))
        {
            backwards.Add(from);
        }
        else
        {
            _backward[to] = [from];
        }
    }

    public IEnumerable<Node> GetNeighbours(Node node)
    {
        if (_forward.TryGetValue(node, out var forwards))
        {
            return forwards;
        }

        return [];
    }
}

public struct Edge<Node>
{
    public required Node From { get; set; }
    public required Node To { get; set; }
}

public class TopoloicalSorter<Node> where Node : IEquatable<Node>
{
    private List<Node> _result = new();
    private readonly HashSet<Node> _permanentMarks = new();
    private readonly HashSet<Node> _temporaryMarks = new();

    public Node[] Sort(DirectedGraph<Node> graph)
    {
        foreach (var vertex in graph.Vertices)
        {
            if (_permanentMarks.Contains(vertex))
            {
                continue;
            }

            Visit(graph, vertex);
        }

        var result = _result.ToArray();

        _result.Clear();
        _permanentMarks.Clear();
        _temporaryMarks.Clear();

        return result;
    }

    private void Visit(DirectedGraph<Node> graph, Node node)
    {
        if (_permanentMarks.Contains(node))
        {
            return;
        }

        if (_temporaryMarks.Contains(node))
        {
            throw new Exception("cycle detected");
        }

        _temporaryMarks.Add(node);

        foreach (var neighbour in graph.GetNeighbours(node))
        {
            Visit(graph, neighbour);
        }

        _permanentMarks.Add(node);

        _result = _result.Prepend(node).ToList();
    }
}