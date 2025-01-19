using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Rebuilders;

public class DirtyBitSetRebuilder<Key, Value> : IRebuilder<Key, Value>
{
    private readonly ILogger _logger;

    private readonly IStore<Key, Value> _store;
    private readonly HashSet<Key> _isDirty;

    public DirtyBitSetRebuilder(
        ILoggerFactory loggerFactory,
        IStore<Key, Value> store,
        HashSet<Key> isDirty)
    {
        _logger = loggerFactory.CreateLogger<DirtyBitSetRebuilder<Key, Value>>();

        _store = store;
        _isDirty = isDirty;
    }

    public async Task<Value> Rebuild(
        Key key,
        IBuildTask<Key, Value> task,
        IBuildSystem<Key, Value> system)
    {
        if (_isDirty.Contains(key))
        {
            _logger.LogInformation("{Key} is dirty", key);

            var newValue = await task.Execute(system);

            _store.Put(key, newValue);
            _isDirty.Remove(key);

            return newValue;
        }
        else
        {
            _logger.LogInformation("{Key} is clean", key);

            return _store.Get(key);
        }
    }
}