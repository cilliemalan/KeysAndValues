using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Internal
{
    internal static class StreamExtensions
    {
        public static bool TryReadExactly(this Stream stream, Span<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                int amt = stream.Read(buffer);
                if (amt == 0)
                {
                    return false;
                }
                buffer = buffer[amt..];
            }

            return true;
        }
    }
}
