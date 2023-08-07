using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.Util
{
    public class MessageArgs
    {
        public readonly Connection Sender;

        public MessageArgs(Connection sender)
        {
            Sender = sender;
        }
    }
}
