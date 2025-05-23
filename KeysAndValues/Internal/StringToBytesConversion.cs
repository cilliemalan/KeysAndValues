using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Internal
{
    internal static class StringToBytesConversion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mem GetBytes(string str)
        {
            return new Mem(Encoding.UTF8.GetBytes(str).AsMemory());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetString(Mem mem)
        {
            return Encoding.UTF8.GetString(mem.Span);
        }
    }
}
