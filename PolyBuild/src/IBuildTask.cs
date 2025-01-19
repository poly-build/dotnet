using System.Threading.Tasks;

namespace PolyBuild;

public interface IBuildTask<Key, Value>
{
    Task<Value> Execute(IBuildSystem<Key, Value> system);
}