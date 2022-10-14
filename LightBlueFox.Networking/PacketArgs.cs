using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
{
    public class PacketArgs
    {
        public readonly Connection Sender;

        public PacketArgs(Connection sender)
        {
            Sender = sender;
        }
    }
}
