using KeysAndValues.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Tests;

public class Sha256Tests
{
    // Helper method to convert a hex string to a byte array
    private static byte[] HexToByteArray(string hex)
    {
        // Remove any spaces from the hex string
        hex = hex.Replace(" ", "");
        // Ensure the hex string has an even number of characters
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have an even number of characters.");
        }

        byte[] byteArray = new byte[hex.Length / 2];
        for (int i = 0; i < byteArray.Length; i++)
        {
            byteArray[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return byteArray;
    }

    [Fact]
    public void Sha256_EmptyString_ComputesCorrectHash()
    {
        // Arrange
        var sha256 = new Sha256(stackalloc uint[24]);
        byte[] input = Encoding.UTF8.GetBytes(""); // Empty string
        byte[] expectedHash = SHA256.HashData([]);
        byte[] actualHash = new byte[32];

        // Act
        sha256.Ingest(input);
        sha256.ComputeHash(actualHash);

        // Assert
        Assert.Equal(expectedHash, actualHash);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(60)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(120)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(250)]
    [InlineData(251)]
    [InlineData(253)]
    [InlineData(254)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(510)]
    [InlineData(511)]
    [InlineData(512)]
    [InlineData(513)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    public void ReferenceTest(int numBytes)
    {
        var r = new Random(213123132);
        var input = new byte[numBytes];
        r.NextBytes(input);

        var sha256 = new Sha256(stackalloc uint[24]);
        byte[] expectedHash = SHA256.HashData(input);
        byte[] ourHash = new byte[32];
        sha256.Ingest(input);
        sha256.ComputeHash(ourHash);
        Assert.Equal(expectedHash, ourHash);
    }

    [Fact]
    public void Sha256_ShortString_ComputesCorrectHash()
    {
        // Arrange
        var sha256 = new Sha256(stackalloc uint[24]);
        byte[] input = Encoding.UTF8.GetBytes("abc");
        byte[] expectedHash = HexToByteArray("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        byte[] actualHash = new byte[32];

        // Act
        sha256.Ingest(input);
        sha256.ComputeHash(actualHash);

        // Assert
        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void Sha256_IngestInChunks_ComputesCorrectHash()
    {
        // Arrange
        var sha256 = new Sha256(stackalloc uint[24]);
        string message = "This is a test message that will be ingested in multiple chunks to verify correct buffering and processing.";
        byte[] fullInput = Encoding.UTF8.GetBytes(message);

        // Expected hash computed using a standard SHA-256 utility (e.g., online calculator or .NET's built-in)
        // For "This is a test message that will be ingested in multiple chunks to verify correct buffering and processing."
        byte[] expectedHash = SHA256.HashData(fullInput);
        byte[] actualHash = new byte[32];

        // Act - Ingest in chunks
        sha256.Ingest(fullInput.AsSpan(0, 10)); // First chunk
        sha256.Ingest(fullInput.AsSpan(10, 25)); // Second chunk
        sha256.Ingest(fullInput.AsSpan(35)); // Remaining chunk

        sha256.ComputeHash(actualHash);

        // Assert
        Assert.Equal(expectedHash, actualHash);
    }

    [Fact]
    public void Sha256_OutputSpanTooSmall_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            // Arrange
            var sha256 = new Sha256(stackalloc uint[24]);
            byte[] input = Encoding.UTF8.GetBytes("test");
            byte[] smallOutput = new byte[31]; // Too small

            // Act & Assert
            sha256.Ingest(input);
            sha256.ComputeHash(smallOutput);
        });
        Assert.Contains("Output span must be at least 32 bytes long", ex.Message);
    }

    [Fact]
    public void Sha256_ComputeHashResetsState_AllowsFurtherIngestion()
    {
        // Arrange
        var sha256 = new Sha256(stackalloc uint[24]);
        byte[] input1 = Encoding.UTF8.GetBytes("hello");
        byte[] input2 = Encoding.UTF8.GetBytes("world");

        // Hash for "hello"
        byte[] expectedHash1 = SHA256.HashData(input1);
        byte[] actualHash1 = new byte[32];

        // Hash for "world"
        byte[] expectedHash2 = SHA256.HashData([.. input1, .. input2]);
        byte[] actualHash2 = new byte[32];

        // Act 1: Compute hash for "hello"
        sha256.Ingest(input1);
        sha256.ComputeHash(actualHash1);

        // Assert 1
        Assert.Equal(expectedHash1, actualHash1);

        sha256.Ingest(input2); // Ingest new data
        sha256.ComputeHash(actualHash2);

        // Assert 2
        Assert.Equal(expectedHash2, actualHash2);
    }
}