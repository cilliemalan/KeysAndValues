using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues
{
    /// <summary>
    /// Provides functionality to write (log) all changes
    /// to a store
    /// </summary>
    public interface IWriteAheadLog
    {
        public void Append(ReadOnlySpan<ChangeOperation> operations, long sequence);
        public void AppendSnapshot(ImmutableAvlTree<Mem, Mem> snapshot, long sequence);
        public Task FlushAsync(CancellationToken cancellationToken);
        public ImmutableAvlTree<Mem, Mem>? GetSnapshot(long sequence);
        public (ImmutableAvlTree<Mem, Mem> Store, long Sequence) GetLatest(long sequence);
        public WriteAheadLogEntry[]? Diff(long sequence1, long sequence2);
        public IEnumerable<WriteAheadLogEntry> Enumerate(long fromSequence, long toSequence);
        public Task TruncateAsync(long sequence);
    }
}
