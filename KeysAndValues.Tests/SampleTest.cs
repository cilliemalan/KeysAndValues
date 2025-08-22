using System.Reflection;

namespace KeysAndValues.Tests;

public class SampleTest
{
    [Fact]
    public void RunSampleTest()
    {
        Assembly.Load("KeysAndValues.Sample")
            .GetType("Program")!
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .First()
            .Invoke(null, [Array.Empty<string>()]);
    }
}
