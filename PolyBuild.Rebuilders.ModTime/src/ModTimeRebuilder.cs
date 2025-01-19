using System;
using System.Collections.Generic;
using System.Collections.Map;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Rebuilders;

public class ModTimeRebuilder<Key, Value> : IRebuilder<Key, Value>
{
    private readonly ILogger _logger;

    private readonly IStore<Key, Value> _store;
    private readonly ITotalMap<Key, Key[]> _dependencies;
    private readonly IDictionary<Key, int> _modificationTimes;
    private int _now;

    public ModTimeRebuilder(
        ILoggerFactory loggerFactory,
        IStore<Key, Value> store,
        ITotalMap<Key, Key[]> dependencies,
        IDictionary<Key, int> modificationTimes,
        int now)
    {
        _logger = loggerFactory.CreateLogger<ModTimeRebuilder<Key, Value>>();

        _store = store;
        _dependencies = dependencies;

        _modificationTimes = modificationTimes;
        _now = now;
    }

    public async Task<Value> Rebuild(Key key, IBuildTask<Key, Value> task, IBuildSystem<Key, Value> system)
    {
        _logger.LogInformation("Building {Key}", key);

        var currentValue = _store.Get(key);

        var dependencies = _dependencies.Get(key);

        var dirty = IsDirty(key, dependencies);
        if (dirty)
        {
            _logger.LogInformation("{Key} is dirty", key);

            _modificationTimes[key] = _now;
            _now += 1;

            var newValue = await task.Execute(system);

            _store.Put(key, newValue);

            return newValue;
        }
        else
        {
            _logger.LogInformation("{Key} is up-to-date", key);

            return currentValue;
        }
    }

    private bool IsDirty(Key key, Key[] dependencies)
    {
        if (_modificationTimes.TryGetValue(key, out var modificationTime))
        {
            foreach (var dependency in dependencies)
            {
                if (_modificationTimes.TryGetValue(dependency, out var dependencyModificationTime))
                {
                    if (dependencyModificationTime > modificationTime)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        else
        {
            return true;
        }
    }
}