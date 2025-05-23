using KeysAndValues.Internal;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace KeysAndValues;

public partial class KeyValueStore
{
    private static void Serialize(IList<ChangeOperation> operations, long sequence, Stream stream)
    {
        var writer = new ArrayBufferWriter<byte>();
        Serialize(operations, sequence, writer);
        stream.Write(writer.WrittenSpan);
    }

    private static void Serialize(ImmutableAvlTree<Mem, Mem> snapshot, long sequence, Stream stream)
    {
        var writer = new ArrayBufferWriter<byte>();
        Serialize(snapshot, sequence, writer);
        stream.Write(writer.WrittenSpan);
    }

    private static void Serialize(IList<ChangeOperation> operations, long sequence, IBufferWriter<byte> writer)
    {
        int bytesRequired = 4 + 1 + 8 + 4 + 32; // length, type, seq, count, hash
        for (int i = 0; i < operations.Count; i++)
        {
            bytesRequired += 1; // Type
            switch (operations[i].Type)
            {
                case ChangeOperationType.None:
                    continue;
                case ChangeOperationType.Set:
                    bytesRequired += 8; // Lengths
                    bytesRequired += operations[i].Key.Length;
                    bytesRequired += operations[i].Value.Length;
                    break;
                case ChangeOperationType.Delete:
                    bytesRequired += 4; // Key length
                    bytesRequired += operations[i].Key.Length;
                    break;
            }
        }
        var span = writer.GetSpan(bytesRequired)[..bytesRequired];
        var dest = span;
        BitConverter.TryWriteBytes(dest, bytesRequired - 4);
        dest[4] = (byte)WriteAheadLogEntryType.ChangeOperation;
        BitConverter.TryWriteBytes(dest[5..], sequence);
        BitConverter.TryWriteBytes(dest[13..], operations.Count);
        dest = dest[17..];

        for (int i = 0; i < operations.Count; i++)
        {
            dest[0] = (byte)operations[i].Type;
            dest = dest[1..];
            switch (operations[i].Type)
            {
                case ChangeOperationType.None:
                    continue;
                case ChangeOperationType.Set:
                    BitConverter.TryWriteBytes(dest, operations[i].Key.Length);
                    dest = dest[4..];
                    operations[i].Key.Span.CopyTo(dest);
                    dest = dest[operations[i].Key.Length..];
                    BitConverter.TryWriteBytes(dest, operations[i].Value.Length);
                    dest = dest[4..];
                    operations[i].Value.Span.CopyTo(dest);
                    break;
                case ChangeOperationType.Delete:
                    BitConverter.TryWriteBytes(dest, operations[i].Key.Length);
                    dest = dest[4..];
                    operations[i].Key.Span.CopyTo(dest);
                    break;
            }
        }

        SHA256.HashData(span[..^32], span[^32..]);
        writer.Advance(bytesRequired);
    }

    private static void Serialize(ImmutableAvlTree<Mem, Mem> snapshot, long sequence, IBufferWriter<byte> writer)
    {
        int bytesRequired = 4 + 1 + 8 + 32; // length, type, seq, hash
        foreach (var entry in snapshot)
        {
            bytesRequired += entry.Key.Length + entry.Value.Length + 8; // key, value, lengths
        }

        var span = writer.GetSpan(bytesRequired)[..bytesRequired];
        var dest = span;
        BitConverter.TryWriteBytes(dest, bytesRequired - 4);
        dest[4] = (byte)WriteAheadLogEntryType.Snapshot;
        BitConverter.TryWriteBytes(dest[5..], sequence);
        dest = dest[13..];
        foreach (var entry in snapshot)
        {
            BitConverter.TryWriteBytes(dest, entry.Key.Length);
            dest = dest[4..];
            entry.Key.Span.CopyTo(dest);
            dest = dest[entry.Key.Length..];
            BitConverter.TryWriteBytes(dest, entry.Value.Length);
            dest = dest[4..];
            entry.Value.Span.CopyTo(dest);
        }
    }
}
