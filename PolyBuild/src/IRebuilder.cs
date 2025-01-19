using System.Threading.Tasks;

namespace PolyBuild;

public interface IRebuilder<Key, Value>
{
    Task<Value> Rebuild(
        Key key,
        IBuildTask<Key, Value> task,
        IBuildSystem<Key, Value> system);
}