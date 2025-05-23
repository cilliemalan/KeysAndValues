namespace KeysAndValues.Internal;

internal static class StringToBytesConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Mem GetBytes(string str)
    {
        return new Mem(Encoding.UTF8.GetBytes(str).AsMemory());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetString(Mem mem)
    {
        return Encoding.UTF8.GetString(mem.Span);
    }
}
