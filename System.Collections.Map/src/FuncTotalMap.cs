namespace System.Collections.Map;

public class FuncTotalMap<Key, Value> : ITotalMap<Key, Value>
{
    private readonly Func<Key, Value> _func;

    public FuncTotalMap(Func<Key, Value> func)
    {
        _func = func;
    }

    public Value Get(Key key)
    {
        return _func.Invoke(key);
    }
}