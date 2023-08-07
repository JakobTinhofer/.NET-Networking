using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.Util
{
    /// <summary>
    /// Describes a handler for when the use of the message buffer is over and it can be released.
    /// </summary>
    /// <param name="message">The message memory</param>
    /// <param name="c">The connection from which the message originated</param>
    public delegate void MessageReleasedHandler(ReadOnlyMemory<byte> message, Connection c);
    
    /// <summary>
    /// This struct is used when messages need to be stored before they can be handled, e.g. when queued up.
    /// </summary>
    public struct MessageStoreHandle
    {
        /// <summary>
        /// The memory handle for the message.
        /// </summary>
        public readonly ReadOnlyMemory<byte> Buffer;

        /// <summary>
        /// Called once the buffer is not in use anymore.
        /// </summary>
        public readonly MessageReleasedHandler? FinishedHandling;
        
        /// <summary>
        /// Creates a new <see cref="MessageStoreHandle"/>.
        /// </summary>
        /// <param name="buffer">Memory handle of the message</param>
        /// <param name="fh">Callback once no longer in use</param>
        public MessageStoreHandle(ReadOnlyMemory<byte> buffer, MessageReleasedHandler? fh)
        {
            Buffer = buffer;
            FinishedHandling = fh;
        }
    }

}
