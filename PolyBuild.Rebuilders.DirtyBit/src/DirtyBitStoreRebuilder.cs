using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyBuild.Rebuilders;

public class DirtyBitRebuilder<Key, Value> : IRebuilder<Key, Value>
{
    private readonly ILogger _logger;

    private readonly IStore<Key, Value> _store;
    private readonly IStore<Key, bool> _isDirty;

    public DirtyBitRebuilder(
        ILoggerFactory loggerFactory,
        IStore<Key, Value> store,
        IStore<Key, bool> isDirty)
    {
        _logger = loggerFactory.CreateLogger<DirtyBitRebuilder<Key, Value>>();

        _store = store;
        _isDirty = isDirty;
    }

    public async Task<Value> Rebuild(
        Key key,
        IBuildTask<Key, Value> task,
        IBuildSystem<Key, Value> system)
    {
        if (_isDirty.Get(key))
        {
            _logger.LogInformation("{Key} is dirty", key);

            var newValue = await task.Execute(system);

            _store.Put(key, newValue);
            _isDirty.Put(key, false);

            return newValue;
        }
        else
        {
            _logger.LogInformation("{Key} is clean", key);

            return _store.Get(key);
        }
    }
}