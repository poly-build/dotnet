namespace System.Collections.Map;

public class DefaultTotalMap<Key, Value> : ITotalMap<Key, Value>
{
    private readonly Value _default;
    private readonly IPartialMap<Key, Value> _map;

    public DefaultTotalMap(IPartialMap<Key, Value> map, Value @default)
    {
        _map = map;
        _default = @default;
    }

    public Value Get(Key key)
    {
        try
        {
            return _map.Get(key);
        }
        catch (MissingKeyException<Key>)
        {
            return _default;
        }
    }
}