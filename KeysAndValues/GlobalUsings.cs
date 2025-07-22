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
