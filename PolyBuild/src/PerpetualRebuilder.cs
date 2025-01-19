using System.Threading.Tasks;

namespace PolyBuild.Rebuilders;

public class PerpetualRebuilder<Key, Value> : IRebuilder<Key, Value>
{
    public async Task<Value> Rebuild(
        Key key,
        IBuildTask<Key, Value> task,
        IBuildSystem<Key, Value> system)
    {
        return await task.Execute(system);
    }
}