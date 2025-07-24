namespace KeysAndValues;

/// <summary>
/// Represents a change from one version of a key value store to the next.
/// </summary>
/// <param name="sequence">The sequence number after the change.</param>
/// <param name="operations">The operations for the change.</param>
public readonly ref struct ChangeBatch(long sequence, ReadOnlySpan<ChangeOperation> operations)
{
    /// <summary>
    /// The sequence number after the change.
    /// </summary>
    public long Sequence { get; } = sequence;

    /// <summary>
    /// The operations for the change.
    /// </summary>
    public ReadOnlySpan<ChangeOperation> Operations { get; } = operations;
}
