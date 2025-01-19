using System.Collections.Generic;
using System.Collections.Map;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Schedulers;

public class SuspendingScheduler<Key, Value> : IBuildSystem<Key, Value>
{
    private readonly ILogger<SuspendingScheduler<Key, Value>> _logger;

    private readonly IRebuilder<Key, Value> _rebuilder;
    private readonly IPartialMap<Key, IBuildTask<Key, Value>> _tasks;
    private readonly IStore<Key, Value> _store;

    public SuspendingScheduler(
        ILoggerFactory loggerFactory,
        IRebuilder<Key, Value> rebuilder,
        IPartialMap<Key, IBuildTask<Key, Value>> tasks,
        IStore<Key, Value> store)
    {
        _logger = loggerFactory.CreateLogger<SuspendingScheduler<Key, Value>>();

        _rebuilder = rebuilder;
        _tasks = tasks;
        _store = store;
    }

    public Task<Value> Build(Key key)
    {
        var tracker = new Tracker(_logger, _rebuilder, _tasks, _store);

        return tracker.Build(key);
    }

    private class Tracker : IBuildSystem<Key, Value>
    {
        private ILogger _logger;

        private readonly IRebuilder<Key, Value> _rebuilder;
        private readonly IPartialMap<Key, IBuildTask<Key, Value>> _tasks;
        private readonly IStore<Key, Value> _store;

        private readonly HashSet<Key> _done = new();

        public Tracker(
            ILogger logger,
            IRebuilder<Key, Value> rebuilder,
            IPartialMap<Key, IBuildTask<Key, Value>> tasks,
            IStore<Key, Value> store)
        {
            _logger = logger;

            _rebuilder = rebuilder;
            _tasks = tasks;
            _store = store;
        }

        public async Task<Value> Build(Key key)
        {
            if (_done.Contains(key))
            {
                _logger.LogInformation("Already built {Key}", key);

                return _store.Get(key);
            }

            _logger.LogInformation("Start building {Key}", key);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var task = _tasks.Get(key);

                var newValue = await _rebuilder.Rebuild(key, task, this);

                _done.Add(key);

                return newValue;
            }
            catch (MissingKeyException<Key>)
            {
                return _store.Get(key);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Took {Duration}ms to build {Key}", stopwatch.ElapsedMilliseconds, key);
            }
        }
    }
}