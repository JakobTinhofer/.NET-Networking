using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking.P2P
{
    internal static class P2PHelpers
    {

        public static IPEndPoint[] ToIPEPs(ReadOnlySpan<byte> packet, int nr)
        {
            var result = new IPEndPoint[nr];
            int p_i = 0;
            for (int i = 0; i < nr; i++)
            {
                int port = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(p_i));
                p_i += 2;

                int addr_len = packet[p_i];
                p_i++;

                IPAddress ip = new IPAddress(packet.Slice(p_i, addr_len));
                p_i += addr_len;

                result[i] = new IPEndPoint(ip, port);
            }
            return result;
        }

        public static byte[] WriteIPEPs(IPEndPoint[] ipeps)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)ipeps.Length);
                foreach (var ep in ipeps)
                {
                    // Write Port in little endian
                    ms.WriteByte((byte)((int)ep.Port & 255));
                    ms.WriteByte((byte)((int)ep.Port >> 8));

                    var addr = ep.Address.GetAddressBytes();

                    // Write Address with length prefix
                    ms.WriteByte((byte)addr.Length);
                    ms.Write(addr);
                }
                return ms.ToArray();
            }
            
        }
    }
}
