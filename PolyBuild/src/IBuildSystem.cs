using System.Threading.Tasks;

namespace PolyBuild;

public interface IBuildSystem<Key, Value>
{
    Task<Value> Build(Key key);
}