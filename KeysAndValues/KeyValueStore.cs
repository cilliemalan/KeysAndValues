using KeysAndValues.Internal;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace KeysAndValues;

public sealed partial class KeyValueStore : IEnumerable<KeyValuePair<Mem, Mem>>
{
    private readonly SpinLock spinLock = new();
    private readonly Log? log;
    private long sequence;
    private ImmutableAvlTree<Mem, Mem> store = ImmutableAvlTree<Mem, Mem>.Empty;

    public bool FlushWalAfterEachWrite { get; set; } = false;

    private KeyValueStore(Stream? writeAheadLogStream)
    {
        if (writeAheadLogStream is not null)
        {
            log = new(writeAheadLogStream);
        }
    }

    public static KeyValueStore CreateEmpty(Stream? writeAheadLog) => new(writeAheadLog);

    public bool TryGet(Mem key, out Mem value)
    {
        return store.TryGetValue(key, out value);
    }

    public long Apply(ChangeOperation[] operations)
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

                var builder = CreateAppliedStore(operations, s);

                bool spinLockTaken = false;
                spinLock.TryEnter(ref spinLockTaken);
                if (!spinLockTaken)
                {
                    Debug.Assert(!FlushWalAfterEachWrite);
                    Thread.Yield();
                    continue;
                }

                bool exchanged = Interlocked.CompareExchange(ref store, builder.ToImmutable(), s) == s;
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
                log!.Flush();
                Monitor.Exit(log);
            }
        }
    }

    private static ImmutableAvlTree<Mem, Mem>.Builder CreateAppliedStore(ChangeOperation[] operations, ImmutableAvlTree<Mem, Mem> s)
    {
        var b = s.ToBuilder();
        for (int i = 0; i < operations.Length; i++)
        {
            var operation = operations[i];

            switch (operation.Type)
            {
                case ChangeOperationType.Set:
                    b[operation.Key] = operation.Value;
                    break;
                case ChangeOperationType.Delete:
                    b.Remove(operation.Key);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operations));
            }
        }

        return b;
    }


    public void FlushWriteAheadLog()
    {
        log?.Flush();
    }

    public Task FlushWriteAheadLogAsync(CancellationToken cancellationToken = default)
    {
        return log?.FlushAsync(cancellationToken) ?? Task.CompletedTask;
    }


    public (long Sequence, IReadOnlyDictionary<Mem, Mem> Store) Snapshot()
    {
        ForceTakeSpinlock();
        var sequence = this.sequence;
        var store = this.store;
        spinLock.Exit();

        return (sequence, store);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ForceTakeSpinlock()
    {
        for (; ; )
        {
            bool taken = false;
            spinLock.Enter(ref taken);
            if (taken)
            {
                break;
            }
            Thread.Yield();
        }
    }
}
