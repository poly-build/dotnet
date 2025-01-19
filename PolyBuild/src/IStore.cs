namespace PolyBuild;

public interface IStore<Key, Value>
{
    bool Contains(Key key);
    Value Get(Key key);
    void Put(Key key, Value value);
    void Remove(Key key);
}