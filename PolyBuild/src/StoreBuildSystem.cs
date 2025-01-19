using System.Threading.Tasks;

namespace PolyBuild;

public class StoreBuildSystem<Key, Value> : IBuildSystem<Key, Value>
{
    private readonly IStore<Key, Value> _store;

    public StoreBuildSystem(IStore<Key, Value> store)
    {
        _store = store;
    }

    public async Task<Value> Build(Key key)
    {
        await Task.CompletedTask;

        return _store.Get(key);
    }
}