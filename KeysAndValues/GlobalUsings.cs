global using System.Text;
global using System.Diagnostics;
global using System.Buffers;
global using System.Runtime.CompilerServices;
global using System.Diagnostics.CodeAnalysis;

#if NETSTANDARD
namespace System.Runtime.CompilerServices
{
    internal class EmbeddedAttribute : Attribute { }

    [Embedded]
    internal class IsExternalInit : Attribute { }
}
#endif

#if NETSTANDARD2_0

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// </summary>
    /// <param name="returnValue"></param>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
    {
        /// <summary>
        /// </summary>
        public bool ReturnValue { get; } = returnValue;
    }
}

namespace System.Text
{
    /// <summary>
    /// </summary>
    public static class EncodingExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="span"></param>
        /// <returns></returns>
        public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> span)
        {
            fixed (byte* ptr = span)
            {
                return encoding.GetString(ptr, span.Length);
            }
        }
    }
}

#endif