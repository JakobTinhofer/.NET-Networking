using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
{
    public delegate void MessageReleasedHandler(ReadOnlyMemory<byte> message, Connection c);
    public struct MessageStoreHandle
    {
        public ReadOnlyMemory<byte> Buffer;
        public MessageReleasedHandler? FinishedHandling;
        public MessageStoreHandle(ReadOnlyMemory<byte> buffer, MessageReleasedHandler? fh)
        {
            Buffer = buffer;
            FinishedHandling = fh;
        }
    }
    
}
