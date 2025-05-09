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
    private static bool TryDeserialize(Stream stream, out int amtRead, [MaybeNullWhen(false)] out ChangeOperation[] operations)
    {
        amtRead = 0;
        operations = null;

        // read the length and type
        Span<byte> header = stackalloc byte[5];
        int r = stream.ReadAtLeast(header, 5, false);
        amtRead += r;
        if (r != 5)
        {
            return false;
        }

        // parse the length and type
        int length = BitConverter.ToInt32(header);
        int type = header[4];
        if (length < 37)
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

        if (type != 1)
        {
            operations = [];
            return true;
        }

        var index = 5;
        int count = BitConverter.ToInt32(buffer[index..]);
        index += 4;
        operations = new ChangeOperation[count];
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

        return true;
    }

    private static void Serialize(IList<ChangeOperation> operations, Stream stream)
    {
        var writer = new SegmentedBufferWriter<byte>();
        Serialize(operations, writer);
        foreach (var mem in writer.WrittenSequence)
        {
            stream.Write(mem.Span);
        }
    }

    private static void Serialize(IList<ChangeOperation> operations, IBufferWriter<byte> writer)
    {
        int bytesRequired = 4 + 1 + 4 + 32; // length, type, count, hash
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
        dest[4] = 1; // type
        BitConverter.TryWriteBytes(dest[5..], operations.Count);
        dest = dest[9..];

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
}
