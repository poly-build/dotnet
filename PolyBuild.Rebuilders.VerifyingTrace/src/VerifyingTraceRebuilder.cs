using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Rebuilders;

public class VerifyingTraceRebuilder<Key, Value> : IRebuilder<Key, Value>
    where Key : notnull
    where Value : IHashable
{
    private readonly ILogger _logger;

    private readonly IVerifyingTraceStore<Key> _traces;
    private readonly IStore<Key, Value> _store;

    public VerifyingTraceRebuilder(
        ILoggerFactory loggerFactory,
        IVerifyingTraceStore<Key> traces,
        IStore<Key, Value> store)
    {
        _logger = loggerFactory.CreateLogger<VerifyingTraceRebuilder<Key, Value>>();

        _traces = traces;
        _store = store;
    }

    public async Task<Value> Rebuild(Key key, IBuildTask<Key, Value> task, IBuildSystem<Key, Value> system)
    {
        _logger.LogInformation("Rebuilding {Key}", key);

        if (_store.Contains(key))
        {
            _logger.LogInformation("{Key} was already computed", key);

            var currentValue = _store.Get(key);

            var upToDate = await _traces.Verify(key, currentValue.GetHash(), async (Key key) =>
            {
                var value = await system.Build(key);

                return value.GetHash();
            });

            if (upToDate)
            {
                _logger.LogInformation("{Key} was up to date", key);

                return currentValue;
            }
        }

        _logger.LogInformation("{Key} needs to be built", key);
        var stopwatch = Stopwatch.StartNew();

        var tracker = new TrackingFetcher(system);

        var newValue = await task.Execute(tracker);

        _traces.Record(key, newValue.GetHash(), tracker.DependencyHashes.ToArray());

        _store.Put(key, newValue);

        stopwatch.Stop();
        _logger.LogInformation("{Key} has been built in {Duration}ms", key, stopwatch.ElapsedMilliseconds);

        return newValue;
    }

    private class TrackingFetcher : IBuildSystem<Key, Value>
    {
        private readonly IBuildSystem<Key, Value> _system;

        public List<Tuple<Key, string>> DependencyHashes { get; set; } = new();

        public TrackingFetcher(IBuildSystem<Key, Value> fetcher)
        {
            _system = fetcher;
        }

        public async Task<Value> Build(Key key)
        {
            var value = await _system.Build(key);

            DependencyHashes.Add(new Tuple<Key, string>(key, value.GetHash()));

            return value;
        }
    }
}

public class DictionaryVerifyingTraceStore<Key, Value> : IVerifyingTraceStore<Key>
    where Key : notnull
{
    private readonly IDictionary<Key, Trace<Key, Value>> _traces;

    public DictionaryVerifyingTraceStore()
    {
        _traces = new Dictionary<Key, Trace<Key, Value>>();
    }

    public DictionaryVerifyingTraceStore(IDictionary<Key, Trace<Key, Value>> traces)
    {
        _traces = traces;
    }

    public void Record(Key key, string hash, Tuple<Key, string>[] dependencyHashes)
    {
        _traces[key] = new Trace<Key, Value>()
        {
            Key = key,
            Hash = hash,
            Depends = dependencyHashes,
        };
    }

    public async Task<bool> Verify(Key key, string hash, Func<Key, Task<string>> fetchHash)
    {
        if (_traces.TryGetValue(key, out var trace))
        {
            foreach (var (dependency, dependencyHash) in trace.Depends)
            {
                var newHash = await fetchHash.Invoke(dependency);

                if (newHash != dependencyHash)
                {
                    return false;
                }
            }

            if (trace.Hash != hash)
            {
                return false;
            }

            return true;
        }
        else
        {
            return false;
        }
    }
}

public interface IVerifyingTraceStore<Key>
{
    void Record(Key key, string hash, Tuple<Key, string>[] dependencyHashes);
    Task<bool> Verify(Key key, string hash, Func<Key, Task<string>> fetchHash);
}

public struct Trace<K, V>
{
    public required K Key { get; set; }
    public required string Hash { get; set; }

    public required Tuple<K, string>[] Depends { get; set; }
}

public interface IHashable
{
    string GetHash();
}