using System;
using System.Collections.Generic;
using System.Collections.Map;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Schedulers;

public class RestartingScheduler<Key, Value> : IBuildSystem<Key, Value>
    where Key : IEquatable<Key>
{
    private readonly ILogger _logger;

    private readonly IPartialMap<Key, IBuildTask<Key, Value>> _tasks;
    private readonly IStore<Key, Value> _store;

    private readonly IRebuilder<Key, Value> _rebuilder;
    public List<Key> Chain { get; set; }

    public RestartingScheduler(
        ILoggerFactory loggerFactory,
        IPartialMap<Key, IBuildTask<Key, Value>> tasks,
        IStore<Key, Value> store,
        IRebuilder<Key, Value> rebuilder,
        List<Key> chain)
    {
        _logger = loggerFactory.CreateLogger<RestartingScheduler<Key, Value>>();

        _rebuilder = rebuilder;
        _tasks = tasks;
        _store = store;

        Chain = chain;
    }

    public async Task<Value> Build(Key target)
    {
        if (!Chain.Contains(target))
        {
            Chain.Add(target);
        }

        var completed = new HashSet<Key>();

        var newChain = new List<Key>();

        await Go(newChain, completed, Chain);

        Chain = newChain;

        return _store.Get(target);
    }

    private async Task Go(List<Key> result, HashSet<Key> completed, List<Key> chain)
    {
        if (chain.Count <= 0)
        {
            return;
        }

        var key = chain[0];
        chain.RemoveAt(0);

        var fetcher = new Fetcher(completed, _store);

        _logger.LogInformation("Start building {Key}", key);

        try
        {
            var task = _tasks.Get(key);

            try
            {
                await _rebuilder.Rebuild(key, task, fetcher);
            }
            catch (MissingDependency e)
            {
                _logger.LogInformation("Missing dependency {Key}", e.Dependency);

                chain = chain.Where(x => !x.Equals(e.Dependency)).ToList();
                chain.Insert(0, e.Dependency);
                chain.Add(key);

                await Go(result, completed, chain);
                return;
            }

            _logger.LogInformation("Built {Key}", key);

            result.Add(key);
            completed.Add(key);

            await Go(result, completed, chain);
            return;
        }
        catch (MissingKeyException<Key> e)
        {
            _logger.LogInformation("Task for {Key} was missing. Marking as completed.", e.MissingKey);

            result.Add(key);
            completed.Add(key);
            await Go(result, completed, chain);
            return;
        }
    }

    private class Fetcher : IBuildSystem<Key, Value>
    {
        private readonly HashSet<Key> _completed;
        private readonly IStore<Key, Value> _store;

        public Fetcher(HashSet<Key> completed, IStore<Key, Value> store)
        {
            _completed = completed;
            _store = store;
        }

        public async Task<Value> Build(Key key)
        {
            await Task.CompletedTask;

            if (_completed.Contains(key))
            {
                return _store.Get(key);
            }
            else
            {
                throw new MissingDependency(key);
            }
        }
    }

    private class MissingDependency : Exception
    {
        public Key Dependency { get; set; }

        public MissingDependency(Key key)
            : base("Missing dependency")
        {
            Dependency = key;
        }
    }
}