using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public class MessageInfo
    {
        public readonly ProtocolConnection From;
        public MessageInfo(ProtocolConnection from)
        {
            From = from;
        }
    }

    
}
