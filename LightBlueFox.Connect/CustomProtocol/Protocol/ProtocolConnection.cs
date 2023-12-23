using LightBlueFox.Connect.Structure;
using LightBlueFox.Connect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public class ProtocolConnection
    {
        public readonly Connection Connection;
        public readonly ProtocolDefinition Protocol;

        public static ProtocolConnection CreateWithValidation(ProtocolDefinition def, Connection con, ConnectionNegotiationPosition pos)
        {
            ConnectionNegotiation.ValidateConnection(con, pos, def.Validator);
            return new ProtocolConnection(def, con);
        }

        public static ProtocolConnection CreateWithoutValidation(ProtocolDefinition def, Connection con)
        {
            return new ProtocolConnection(def, con);
        }

        private ProtocolConnection(ProtocolDefinition prot, Connection con)
        {
            Connection = con;
            Protocol = prot;
            Connection.MessageHandler = (d, arg) => prot.MessageHandler(d, arg, this);
        }

        public void WriteMessage<T>(T message)
        {
            Protocol.SendMessage(message, this);
        }
    }
}
