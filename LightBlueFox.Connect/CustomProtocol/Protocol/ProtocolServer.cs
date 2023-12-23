using LightBlueFox.Connect.Structure;
using LightBlueFox.Connect.Structure.ConnectionSources;
using LightBlueFox.Connect.Structure.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public class ProtocolServer
    {
        private Server wrappedServer;
        public readonly ProtocolDefinition Protocol;

        private Dictionary<Connection, ProtocolConnection> connections = new();
        public IReadOnlyList<ProtocolConnection> Connections {  get { return connections.Values.ToList(); } }

        public ProtocolServer(ProtocolDefinition prot, ConnectionSource[] sources, ConnectionValidator[]? validators = null) {
            Protocol = prot;
            
            if(validators == null) validators = new ConnectionValidator[1] { prot.Validator };
            else validators = validators.Append(prot.Validator).ToArray();

            wrappedServer = new Server(validators, sources);
            OnConnectionDisconnected += handleConnectionDisconnected;
            wrappedServer.MessageHandler += (d, a) => { Protocol.MessageHandler(d, a, connections[a.Sender]); };
            wrappedServer.OnConnectionValidated += handleConnectionValidated;
        }

        public event ConnectionDisconnectedHandler OnConnectionDisconnected
        {
            add
            {
                wrappedServer.OnConnectionDisconnected += value;
            }
            remove { 
                wrappedServer.OnConnectionDisconnected -= value;
            }
        }

        private void handleConnectionValidated(Connection c, Server sender)
        {
            connections.Add(c, ProtocolConnection.CreateWithoutValidation(Protocol, c));
        }

        private void handleConnectionDisconnected(Connection c, Exception? ex)
        {
            connections.Remove(c);
        }
    }
}
