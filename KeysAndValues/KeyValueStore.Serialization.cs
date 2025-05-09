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
    internal static bool TryDeserializeLogEntry(Stream stream, out int amtRead, out Log.LogEntry entry)
    {
        amtRead = 0;
        entry = default;

        // read the length and type
        Span<byte> header = stackalloc byte[4 + 1 + 8];
        int r = stream.ReadAtLeast(header, header.Length, false);
        amtRead += r;
        if (r != header.Length)
        {
            return false;
        }

        // parse the length, type, and sequence
        int length = BitConverter.ToInt32(header);
        int type = header[4];
        long sequence = BitConverter.ToInt64(header[5..]);
        if (length < 32 + header.Length || sequence < 0)
        {
            return false;
        }

        // read the whole buffer
        var bdata = new byte[length];
        var buffer = new byte[length].AsSpan();
        header.CopyTo(buffer);
        r = stream.ReadAtLeast(buffer[header.Length..], length - header.Length, false);
        amtRead += r;
        if (r != length - header.Length)
        {
            return false;
        }

        // check the hash
        Span<byte> hashcmp = stackalloc byte[32];
        SHA256.HashData(buffer[..^32], hashcmp);
        if (!hashcmp.SequenceEqual(buffer[^32..]))
        {
            return false;
        }

        var letype = (Log.LogEntryType)type;
        switch (letype)
        {
            case Log.LogEntryType.ChangeOperation:
                {
                    var index = 13;
                    int count = BitConverter.ToInt32(buffer[index..]);
                    index += 4;
                    var operations = new ChangeOperation[count];
                    for (int i = 0; i < count; i++)
                    {
                        var optype = (ChangeOperationType)buffer[index++];
                        ReadOnlyMemory<byte> key;
                        ReadOnlyMemory<byte> value;
                        switch (operations[i].Type)
                        {
                            case ChangeOperationType.Set:
                                int kl = BitConverter.ToInt32(buffer[index..]);
                                index += 4;
                                key = new(bdata, index, kl);
                                index += kl;
                                int vl = BitConverter.ToInt32(buffer[index..]);
                                index += 4;
                                value = new(bdata, index, vl);
                                index += vl;
                                break;
                            case ChangeOperationType.Delete:
                                int dkl = BitConverter.ToInt32(buffer[index..]);
                                index += 4;
                                key = new ReadOnlyMemory<byte>(bdata, index, dkl);
                                index += dkl;
                                value = default;
                                break;
                            default:
                            case ChangeOperationType.None:
                                key = default;
                                value = default;
                                break;
                        }
                        operations[i] = new ChangeOperation
                        {
                            Type = optype,
                            Key = key,
                            Value = value
                        };
                    }

                    entry = new()
                    {
                        Type = letype,
                        Sequence = sequence,
                        ChangeOperations = operations
                    };
                    return true;
                }
            case Log.LogEntryType.Snapshot:
                {
                    // note: reading it like this prevents the 
                    // entire block from ever being GC'd
                    // which is fine as long as no dictionaries
                    // are built based on it.
                    var index = 13;
                    var builder = ImmutableAvlTree<Mem, Mem>.Empty.ToBuilder();
                    while (index < length - 32)
                    {
                        int kl = BitConverter.ToInt32(buffer[index..]);
                        index += 4;
                        var key = new ReadOnlyMemory<byte>(bdata, index, kl);
                        index += kl;
                        int vl = BitConverter.ToInt32(buffer[index..]);
                        index += 4;
                        var value = new ReadOnlyMemory<byte>(bdata, index, vl);
                        index += vl;
                        builder.Add(key, value);
                    }
                    entry = new() { Type = letype, Sequence = sequence, Snapshot = builder.ToImmutable() };
                }
                return true;
            default:
                entry = new() { Type = letype, Sequence = sequence };
                return true;
        }
    }

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
        dest[4] = (byte)Log.LogEntryType.ChangeOperation;
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
        dest[4] = (byte)Log.LogEntryType.Snapshot;
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
