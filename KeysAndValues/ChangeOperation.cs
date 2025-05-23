namespace KeysAndValues;

public readonly struct ChangeOperation
{
    public ChangeOperationType Type { get; init; }
    public ReadOnlyMemory<byte> Key { get; init; }
    public ReadOnlyMemory<byte> Value { get; init; }
}
