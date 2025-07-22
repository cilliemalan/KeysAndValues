using KeysAndValues.Internal;
using System.Security.Cryptography;

namespace KeysAndValues;

public class LogSerialization
{
    public static bool TryReadEntry(Stream stream, out WriteAheadLogEntry entry)
    {
        entry = default;
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        if (!stream.TryReadExactly(tmp[..12]))
        {
            return false;
        }
        hasher.AppendData(tmp[..12]);
        long sequence = BitConverter.ToInt64(tmp[..8]);
        if (sequence <= 0)
        {
            return false;
        }

        int numOps = BitConverter.ToInt32(tmp[8..12]);
        if (numOps < 0)
        {
            return false;
        }

        var changes = new ChangeOperation[numOps];
        for (int i = 0; i < numOps; i++)
        {
            // type
            if (!stream.TryReadExactly(tmp[..1]))
            {
                return false;
            }
            hasher.AppendData(tmp[..1]);

            var type = (ChangeOperationType)tmp[0];
            if (type == ChangeOperationType.None)
            {
                continue;
            }

            // key
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int keyLength = BitConverter.ToInt32(tmp[..4]);
            if (keyLength < 0)
            {
                return false;
            }
            var keyMem = new byte[keyLength];
            if (!stream.TryReadExactly(keyMem))
            {
                return false;
            }
            hasher.AppendData(keyMem);

            if (type == ChangeOperationType.Delete)
            {
                changes[i] = new() { Type = type, Key = keyMem };
                continue;
            }

            if (type != ChangeOperationType.Set)
            {
                return false;
            }

            // value
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int valueLength = BitConverter.ToInt32(tmp[..4]);
            if (valueLength < 0)
            {
                return false;
            }
            var valueMem = new byte[valueLength];
            if (!stream.TryReadExactly(valueMem))
            {
                return false;
            }
            hasher.AppendData(valueMem);


            changes[i] = new()
            {
                Type = type,
                Key = keyMem,
                Value = valueMem
            };
        }

        hasher.GetHashAndReset(tmp);
        Span<byte> storedHash = stackalloc byte[32];
        if (!stream.TryReadExactly(storedHash))
        {
            return false;
        }

        if (!storedHash.SequenceEqual(tmp))
        {
            return false;
        }

        entry = new WriteAheadLogEntry
        {
            Sequence = sequence,
            ChangeOperations = changes
        };
        return true;
    }

    internal static bool VerifyNextEntry(Stream stream, out long sequence)
    {
        sequence = 0;
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        if (!stream.TryReadExactly(tmp[..12]))
        {
            return false;
        }
        hasher.AppendData(tmp[..12]);
        sequence = BitConverter.ToInt64(tmp[..8]);
        int numOps = BitConverter.ToInt32(tmp[8..12]);
        if (numOps < 0)
        {
            return false;
        }

        var pool = ArrayPool<byte>.Shared;
        for (int i = 0; i < numOps; i++)
        {
            // type
            if (!stream.TryReadExactly(tmp[..1]))
            {
                return false;
            }
            hasher.AppendData(tmp[..1]);

            var type = (ChangeOperationType)tmp[0];
            if (type == ChangeOperationType.None)
            {
                continue;
            }

            // key
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int keyLength = BitConverter.ToInt32(tmp[..4]);
            if (keyLength < 0)
            {
                return false;
            }
            var keyMem = pool.Rent(keyLength);
            var key = keyMem.AsSpan(0, keyLength);
            if (!stream.TryReadExactly(key))
            {
                pool.Return(keyMem);
                return false;
            }
            hasher.AppendData(key);
            pool.Return(keyMem);

            if (type == ChangeOperationType.Delete)
            {
                continue;
            }

            if (type != ChangeOperationType.Set)
            {
                return false;
            }

            // value
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int valueLength = BitConverter.ToInt32(tmp[..4]);
            if (valueLength < 0)
            {
                return false;
            }
            var valueMem = pool.Rent(valueLength);
            var value = valueMem.AsSpan(0, valueLength);
            if (!stream.TryReadExactly(value))
            {
                pool.Return(valueMem);
                return false;
            }
            hasher.AppendData(value);
            pool.Return(valueMem);
        }

        hasher.GetHashAndReset(tmp);
        Span<byte> storedHash = stackalloc byte[32];
        if (!stream.TryReadExactly(storedHash))
        {
            return false;
        }

        if (!storedHash.SequenceEqual(tmp))
        {
            return false;
        }

        return true;
    }

    public static void WriteEntry(Stream stream, in WriteAheadLogEntry entry)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        BitConverter.TryWriteBytes(tmp, entry.Sequence);
        BitConverter.TryWriteBytes(tmp[8..], entry.ChangeOperations.Length);
        stream.Write(tmp[..12]);
        hasher.AppendData(tmp[..12]);

        for (int i = 0; i < entry.ChangeOperations.Length; i++)
        {
            var change = entry.ChangeOperations[i];
            tmp[0] = (byte)change.Type;
            stream.Write(tmp[0..1]);
            hasher.AppendData(tmp[0..1]);
            if (change.Type == ChangeOperationType.None)
            {
                // shrug
                continue;
            }

            BitConverter.TryWriteBytes(tmp[0..4], change.Key.Length);
            stream.Write(tmp[0..4]);
            hasher.AppendData(tmp[0..4]);
            stream.Write(change.Key.Span);
            hasher.AppendData(change.Key.Span);

            if (change.Type != ChangeOperationType.Set)
            {
                continue;
            }

            BitConverter.TryWriteBytes(tmp[0..4], change.Value.Length);
            stream.Write(tmp[0..4]);
            hasher.AppendData(tmp[0..4]);
            stream.Write(change.Value.Span);
            hasher.AppendData(change.Value.Span);
        }

        hasher.GetHashAndReset(tmp);
        stream.Write(tmp);
    }

}
