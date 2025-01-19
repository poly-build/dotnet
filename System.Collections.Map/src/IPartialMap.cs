using System;

namespace System.Collections.Map;

public interface IPartialMap<Key, Value>
{
    /// <exception cref="MissingFieldException">
    /// Thrown when key is missing.
    /// </exception>
    Value Get(Key key);
}

public class MissingKeyException<Key> : Exception
{
    public Key MissingKey { get; }

    public MissingKeyException(Key key)
        : base("Could not find key")
    {
        MissingKey = key;
    }
}
