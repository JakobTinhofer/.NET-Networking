﻿using LightBlueFox.Connect.Structure;
using LightBlueFox.Connect.Structure.ConnectionSources;
using LightBlueFox.Connect.Structure.Validators;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// Accepts connections from given connection sources, validates the protocol definitions and wraps them in a <see cref="ProtocolConnection"/>.
    /// </summary>
    public class ProtocolServer
    {
        /// <summary>
        /// The protocol used by all connections of this server.
        /// </summary>
        public readonly ProtocolDefinition Protocol;

        /// <summary>
        /// The list of connected clients. Does not contain connections currently in the process of validation.
        /// </summary>
        public IReadOnlyList<ProtocolConnection> Connections { get { return connections.Values.ToList(); } }

        /// <summary>
        /// Creates a new ProtocolServer.
        /// </summary>
        /// <param name="prot">The protocol definition for the server and all its clients.</param>
        /// <param name="sources">A collection of sources from which the server should accept connections.</param>
        /// <param name="validators">A collection of additional validators that are checked on top of the Protocol validation (optional).</param>
        public ProtocolServer(ProtocolDefinition prot, ConnectionSource[] sources, ConnectionValidator[]? validators = null)
        {
            Protocol = prot;

            if (validators == null) validators = new ConnectionValidator[1] { prot.Validator };
            else validators = validators.Append(prot.Validator).ToArray();

            wrappedServer = new Server(validators, sources);
            wrappedServer.OnConnectionDisconnected += handleConnectionDisconnected;
            OnConnectionDisconnected += handleConnectionDisconnected;
            wrappedServer.MessageHandler += (d, a) => { Protocol.MessageHandler(d, a, connections[a.Sender]); };
            wrappedServer.OnConnectionValidated += handleConnectionValidated;
        }

        /// <summary>
        /// Triggered when an already established & validated <see cref="ProtocolConnection"/> terminates the connection for any reason.
        /// </summary>
        public event ConnectionDisconnectedHandler OnConnectionDisconnected
        {
            add
            {
                wrappedServer.OnConnectionDisconnected += value;
            }
            remove
            {
                wrappedServer.OnConnectionDisconnected -= value;
            }
        }


        private Server wrappedServer;
        private Dictionary<Connection, ProtocolConnection> connections = new();

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
