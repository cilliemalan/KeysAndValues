using KeysAndValues.Internal;
using System.Text;

namespace KeysAndValues.Tests;

public class BufferedWriteStreamTests
{
    [Fact]
    public async Task BasicTest()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");

        using var stream = new MemoryStream();
        await using (var bufferedStream = new BufferedWriteStream(stream, true))
        {
            bufferedStream.Write(data);
        }
        Assert.Equal(data, stream.ToArray());
    }

    [Fact]
    public async Task MultiWriteTest()
    {
        var data = Encoding.UTF8.GetBytes("Hello, World!");

        using var stream = new MemoryStream();
        await using (var bufferedStream = new BufferedWriteStream(stream, true))
        {
            for (int i = 0; i < 10; i++)
            {
                bufferedStream.Write(data);
            }
        }

        var s = data.AsSpan();
        while (s.Length > 0)
        {
            Assert.Equal(data.AsSpan(), s[..data.Length]);
            s = s[data.Length..];
        }
    }
}
