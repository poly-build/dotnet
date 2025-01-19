using System.Collections.Generic;

namespace System.Collections.Map;

public class DictionaryMap<Key, Value> : IPartialMap<Key, Value> where Key : notnull
{
    private readonly IDictionary<Key, Value> _lookup;

    public DictionaryMap()
    {
        _lookup = new Dictionary<Key, Value>();
    }

    public Value Get(Key key)
    {
        if (_lookup.TryGetValue(key, out var value))
        {
            return value;
        }
        else
        {
            throw new MissingKeyException<Key>(key);
        }
    }

    public void Add(Key key, Value value)
    {
        _lookup.Add(key, value);
    }
}