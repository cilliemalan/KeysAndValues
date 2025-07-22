namespace KeysAndValues;

/// <summary>
/// The type of a change operation.
/// </summary>
public enum ChangeOperationType
{
    /// <summary>
    /// There is no change. This is an invalid operation type.
    /// </summary>
    None,

    /// <summary>
    /// A set operation.
    /// </summary>
    Set,

    /// <summary>
    /// A delete operation.
    /// </summary>
    Delete
}
