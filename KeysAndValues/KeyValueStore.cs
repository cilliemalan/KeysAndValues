namespace KeysAndValues;

public sealed partial class KeyValueStore
{
    private SpinLock spinLock = new();
    private readonly IWriteAheadLog? log;
    private long sequence;
    private ImmutableAvlTree<Mem, Mem> store = ImmutableAvlTree<Mem, Mem>.Empty;

    public int Count => store.Count;

    public bool FlushWalAfterEachWrite { get; set; } = false;

    private KeyValueStore(IWriteAheadLog? writeAheadLog)
    {
        log = writeAheadLog;
    }

    public static KeyValueStore CreateEmpty()
        => new(null);

    public static KeyValueStore CreateNewFrom(IEnumerable<KeyValuePair<Mem, Mem>> source)
    {
        return new KeyValueStore(null)
        {
            store = ImmutableAvlTree<Mem, Mem>.Empty.AddRange(source, true, false),
            sequence = 1// shrug
        };
    }

    public static KeyValueStore CreateEmptyWithWriteAhead(IWriteAheadLog writeAheadLog)
        => new(writeAheadLog ?? throw new ArgumentNullException(nameof(writeAheadLog)));

    public bool TryGet(Mem key, out Mem value)
    {
        return store.TryGetValue(key, out value);
    }

    public long Apply(ReadOnlySpan<ChangeOperation> operations)
    {
        bool mustFlushAndExit = false;
        if (FlushWalAfterEachWrite && log is not null)
        {
            Monitor.Enter(log, ref mustFlushAndExit);
        }

        try
        {
            for (; ; )
            {
                var s = store;

                bool spinLockTaken = false;
                spinLock.TryEnter(ref spinLockTaken);
                if (!spinLockTaken)
                {
                    Debug.Assert(!FlushWalAfterEachWrite);
                    Thread.Yield();
                    continue;
                }

                var newStore = s.Apply(operations);
                bool exchanged = Interlocked.CompareExchange(ref store, newStore, s) == s;
                if (!exchanged)
                {
                    spinLock.Exit();
                    Debug.Assert(!FlushWalAfterEachWrite);
                    continue;
                }

                // NOTE: Using interlocked because I don't believe the spinlock
                // will do a memory barrier.
                var newSequence = Interlocked.Increment(ref sequence);
                log?.Append(operations, newSequence);

                spinLock.Exit();

                return newSequence;
            }
        }
        finally
        {
            if (mustFlushAndExit)
            {
                _ = log!.FlushAsync(default);
                Monitor.Exit(log);
            }
        }
    }

    public long Sequence => sequence;

    public ImmutableAvlTree<Mem, Mem> Snapshot() => store;

    public ImmutableAvlTree<Mem, Mem> Snapshot(out long sequence)
    {
        bool taken = false;
        spinLock.Enter(ref taken);
        Debug.Assert(taken);
        sequence = this.sequence;
        var store = this.store;
        spinLock.Exit();

        return store;
    }
}
