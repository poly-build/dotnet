using System.Collections.Map;
using System.Threading.Tasks;

namespace PolyBuild;

public class BusyScheduler<Key, Value> : IScheduler<Key, Value>
{
    private readonly IPartialMap<Key, IBuildTask<Key, Value>> _tasks;
    private readonly IStore<Key, Value> _store;

    public BusyScheduler(
        IPartialMap<Key, IBuildTask<Key, Value>> tasks,
        IStore<Key, Value> store)
    {
        _tasks = tasks;
        _store = store;
    }

    public async Task<Value> Build(Key key)
    {
        try
        {
            var task = _tasks.Get(key);

            var value = await task.Execute(this);

            _store.Put(key, value);

            return value;
        }
        catch (MissingKeyException<string>)
        {
            return _store.Get(key);
        }
    }
}