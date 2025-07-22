namespace KeysAndValues;

/// <summary>
/// A change operation.
/// </summary>
public readonly struct ChangeOperation
{
    /// <summary>
    /// The type of the operation.
    /// </summary>
    public ChangeOperationType Type { get; init; }

    /// <summary>
    /// The key
    /// </summary>
    public ReadOnlyMemory<byte> Key { get; init; }

    /// <summary>
    /// The value
    /// </summary>
    public ReadOnlyMemory<byte> Value { get; init; }
}
