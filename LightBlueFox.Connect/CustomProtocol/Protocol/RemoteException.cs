using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public class RemoteException : Exception
    {
        public ProtocolConnection ThrownBy;
        public RemoteErrorType Type;

        public RemoteException(ProtocolConnection c, string? msg, RemoteErrorType t, Exception? innerExc) : base(msg, innerExc)
        {
            ThrownBy = c;
            Type = t;
        }

    }

    public enum RemoteErrorType
    {
        Unknown,
        UnknownMessageType,
        MessageHandlerError,
        ProtocolError,
    }
}
