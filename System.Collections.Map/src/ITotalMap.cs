namespace System.Collections.Map;

public interface ITotalMap<Key, Value>
{
    Value Get(Key key);
}
