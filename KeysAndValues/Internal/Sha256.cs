using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Internal;

public ref struct Sha256
{
    // SHA-256 initial hash values (H0 to H7)
    private static readonly uint[] _initialHashValues =
    [
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
        0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
    ];

    // SHA-256 round constants (K0 to K63)
    private static readonly uint[] _k =
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    ];

    private readonly Span<uint> _h;// H0 to H7
    private readonly Span<byte> _buffer;  // Current 64-byte block buffer
    private int _bufferLength; // Number of bytes currently in _buffer
    private int _totalBytes; // Total number of bytes ingested

    public readonly int TotalBytes => _totalBytes;

    public Sha256(Span<uint> buffer)
    {
        if (buffer.Length < 24)
        {
            throw new InvalidOperationException("tmp must be at least 96 bytes");
        }

        _h = buffer[0..8];
        _buffer = MemoryMarshal.AsBytes(buffer[8..24]);
        _initialHashValues.AsSpan().CopyTo(_h);
        _bufferLength = 0;
        _totalBytes = 0;
    }

    public void Ingest(ReadOnlySpan<byte> data)
    {
        _totalBytes += data.Length;

        int dataIndex = 0;
        if (_bufferLength > 0)
        {
            // Fill the remaining part of the buffer
            int bytesToCopy = Math.Min(data.Length, 64 - _bufferLength);
            data[..bytesToCopy].CopyTo(_buffer[_bufferLength..]);
            _bufferLength += bytesToCopy;
            dataIndex += bytesToCopy;

            if (_bufferLength == 64)
            {
                ProcessBlock(_buffer);
                _bufferLength = 0;
            }
        }

        // Process full blocks directly from the input data
        while (dataIndex + 64 <= data.Length)
        {
            ProcessBlock(data.Slice(dataIndex, 64));
            dataIndex += 64;
        }

        // Store any remaining data in the buffer
        if (dataIndex < data.Length)
        {
            data[dataIndex..].CopyTo(_buffer[_bufferLength..]);
            _bufferLength += data.Length - dataIndex;
        }
    }

    public void ComputeHash(Span<byte> output)
    {
        if (output.Length < 32)
        {
            throw new ArgumentException("Output span must be at least 32 bytes long", nameof(output));
        }

        // Save current state to handle multiple ComputeHash calls if needed
        Span<uint> currentH = stackalloc uint[8];
        _h.CopyTo(currentH);
        Span<byte> currentBuffer = stackalloc byte[64];
        _buffer.CopyTo(currentBuffer);
        int currentBufferLength = _bufferLength;
        int currentTotalBytes = _totalBytes;

        // --- Padding ---
        // Append a single '1' bit
        _buffer[_bufferLength] = 0x80;
        _bufferLength++;

        // If not enough space for length (8 bytes) plus '1' bit, process current buffer
        if (_bufferLength > 56)
        {
            // Fill remaining with zeros
            _buffer[_bufferLength..64].Clear();
            ProcessBlock(_buffer);
            _bufferLength = 0;
        }

        // Fill remaining with zeros up to 56 bytes
        _buffer[_bufferLength..56].Clear();

        // Append the 64-bit message length (in bits, big-endian)
        ulong messageLengthBits = (ulong)currentTotalBytes * 8; // Use saved totalBytes
        _buffer[56] = (byte)((messageLengthBits >> 56) & 0xFF);
        _buffer[57] = (byte)((messageLengthBits >> 48) & 0xFF);
        _buffer[58] = (byte)((messageLengthBits >> 40) & 0xFF);
        _buffer[59] = (byte)((messageLengthBits >> 32) & 0xFF);
        _buffer[60] = (byte)((messageLengthBits >> 24) & 0xFF);
        _buffer[61] = (byte)((messageLengthBits >> 16) & 0xFF);
        _buffer[62] = (byte)((messageLengthBits >> 8) & 0xFF);
        _buffer[63] = (byte)(messageLengthBits & 0xFF);

        ProcessBlock(_buffer);

        // --- Output Hash ---
        for (int i = 0; i < 8; i++)
        {
            WriteUInt32BigEndian(output[(i * 4)..], _h[i]);
        }

        // Restore state for potential further ingestion
        currentH.CopyTo(_h);
        currentBuffer.CopyTo(_buffer);
        _bufferLength = currentBufferLength;
        _totalBytes = currentTotalBytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateRight(uint value, int bits)
    {
        return (value >> bits) | (value << (32 - bits));
    }

    // SHA-256 functions
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Sigma0(uint x) => RotateRight(x, 2) ^ RotateRight(x, 13) ^ RotateRight(x, 22);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Sigma1(uint x) => RotateRight(x, 6) ^ RotateRight(x, 11) ^ RotateRight(x, 25);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Ch(uint x, uint y, uint z) => (x & y) ^ (~x & z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Maj(uint x, uint y, uint z) => (x & y) ^ (x & z) ^ (y & z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Gamma0(uint x) => RotateRight(x, 7) ^ RotateRight(x, 18) ^ (x >> 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Gamma1(uint x) => RotateRight(x, 17) ^ RotateRight(x, 19) ^ (x >> 10);

    private readonly void ProcessBlock(ReadOnlySpan<byte> block)
    {
        // Prepare message schedule W (64 words)
        Span<uint> w = stackalloc uint[64];

        // First 16 words directly from the block (big-endian)
        for (int i = 0; i < 16; i++)
        {
            w[i] = (uint)(block[i * 4] << 24 |
                          block[i * 4 + 1] << 16 |
                          block[i * 4 + 2] << 8 |
                          block[i * 4 + 3]);
        }

        // Extend to 64 words
        for (int i = 16; i < 64; i++)
        {
            w[i] = Gamma1(w[i - 2]) + w[i - 7] + Gamma0(w[i - 15]) + w[i - 16];
        }

        // Initialize working variables
        uint a = _h[0];
        uint b = _h[1];
        uint c = _h[2];
        uint d = _h[3];
        uint e = _h[4];
        uint f = _h[5];
        uint g = _h[6];
        uint h = _h[7];

        // Compression loop
        for (int i = 0; i < 64; i++)
        {
            uint s1 = Sigma1(e);
            uint ch = Ch(e, f, g);
            uint temp1 = h + s1 + ch + _k[i] + w[i];

            uint s0 = Sigma0(a);
            uint maj = Maj(a, b, c);
            uint temp2 = s0 + maj;

            h = g;
            g = f;
            f = e;
            e = d + temp1;
            d = c;
            c = b;
            b = a;
            a = temp1 + temp2;
        }

        // Update hash values
        _h[0] += a;
        _h[1] += b;
        _h[2] += c;
        _h[3] += d;
        _h[4] += e;
        _h[5] += f;
        _h[6] += g;
        _h[7] += h;
    }

    // Helper to write a UInt32 to a span in big-endian order
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt32BigEndian(Span<byte> destination, uint value)
    {
        destination[0] = (byte)((value >> 24) & 0xFF);
        destination[1] = (byte)((value >> 16) & 0xFF);
        destination[2] = (byte)((value >> 8) & 0xFF);
        destination[3] = (byte)(value & 0xFF);
    }
}
