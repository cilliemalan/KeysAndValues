namespace KeysAndValues;

public enum WriteAheadLogEntryType
{
        ChangeOperation = 1,
        Snapshot = 2,
        // note: snapshot must be greater than change operaion
}
