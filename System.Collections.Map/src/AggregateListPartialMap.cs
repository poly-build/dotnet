using System.Collections.Generic;

namespace System.Collections.Map;

public class AggregateListPartialMap<Key, Value> : IPartialMap<Key, Value>
{
    private readonly List<IPartialMap<Key, Value>> _maps;

    public AggregateListPartialMap()
    {
        _maps = new List<IPartialMap<Key, Value>>();
    }

    public void Add(IPartialMap<Key, Value> map)
    {
        _maps.Add(map);
    }

    public Value Get(Key key)
    {
        foreach (var map in _maps)
        {
            try
            {
                return map.Get(key);
            }
            catch (MissingKeyException<Key>)
            {
                continue;
            }
        }

        throw new MissingKeyException<Key>(key);
    }
}