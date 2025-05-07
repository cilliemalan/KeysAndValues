namespace KeysAndValues.Tests;

/// <summary>
/// An equality comparer that considers all values to be equal.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class EverythingEqual<T> : IEqualityComparer<T>, System.Collections.IEqualityComparer
{
    private static EverythingEqual<T> s_singleton = new EverythingEqual<T>();

    private EverythingEqual() { }

    internal static EverythingEqual<T> Default
    {
        get
        {
            return s_singleton;
        }
    }

    public bool Equals(T? x, T? y)
    {
        return true;
    }

    public int GetHashCode(T obj)
    {
        return 1;
    }

    bool System.Collections.IEqualityComparer.Equals(object? x, object? y)
    {
        return true;
    }

    int System.Collections.IEqualityComparer.GetHashCode(object? obj)
    {
        return 1;
    }
}
