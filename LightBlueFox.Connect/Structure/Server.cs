using LightBlueFox.Connect.Structure.ConnectionSources;
using LightBlueFox.Connect.Structure.Validators;
using LightBlueFox.Connect.Util;
using System.Diagnostics;

namespace LightBlueFox.Connect.Structure
{
    /// <summary>
    /// Describes a handler for the <see cref="Server.OnConnectionValidated"/> event, which is called whenever a new client has been successfully validated.
    /// </summary>
    /// <param name="c">The client connection that was validated.</param>
    /// <param name="s">The server that sent the event.</param>
    public delegate void ValidatedClientHandler(Connection c, Server s);
    /// <summary>
    /// Describes a handler for the <see cref="Server.OnValidationFailed"/> event, which is called whenever a new client failed to validate.
    /// </summary>
    /// <param name="c">The client connection that failed validation.</param>
    /// <param name="s">The server that sent the event.</param>
    /// <param name="ex">The reason for the validation failure.</param>
    public delegate void ValidatingClientFailedHandler(Connection c, Server s, ValidationFailedException ex);

    /// <summary>
    /// This class is used to manage multiple connections at once in a classic client-server setting.
    /// </summary>
    public class Server : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Create a new server with multiple validators and connection sources.
        /// </summary>
        /// <param name="validators">The validators to check for every new connection. Note that these need to be in the same order as on the client side.</param>
        /// <param name="sources">The connection sources where the server should accept new connections from.</param>
        public Server(ConnectionValidator[] validators, params ConnectionSource[] sources)
        {
            Validators = validators;
            foreach (ConnectionSource source in sources)
            {
                source.OnNewConnection = NewConnection;
                connectionSources.Add(source);
            }
        }

        /// <summary>
        /// Create a new server with a validator and connection sources.
        /// </summary>
        /// <param name="validators">The validators to check for every new connection. Note that these need to be in the same order as on the client side.</param>
        /// <param name="sources">The connection sources where the server should accept new connections from.</param>
        public Server(ConnectionValidator validator, params ConnectionSource[] sources) : this(new ConnectionValidator[1] { validator }, sources) { }

        #endregion

        #region Private Members
        private readonly List<ConnectionSource> connectionSources = new();
        private readonly List<Connection> validatedConnections = new();
        private readonly List<ConnectionNegotiation> ongoingNegotiations = new();
        #endregion

        #region Public Members
        /// <summary>
        /// Get the currently validated clients that are connected to this server. Note that this does not include connections currently being validated.
        /// </summary>
        public IReadOnlyCollection<Connection> Connections { get
            {
                return validatedConnections.AsReadOnly();
            } 
        }

        /// <summary>
        /// The validators to check for every new connection. Note that these need to be in the same order as on the client side.
        /// </summary>
        public ConnectionValidator[] Validators;

        /// <summary>
        /// The server can manage connections of multiple different types from multiple different sources.
        /// </summary>
        public IReadOnlyCollection<ConnectionSource> ConnectionSources
        {
            get
            {
                return connectionSources.AsReadOnly();
            }
        }

        private MessageHandler? _msgHandler = null;
        
        /// <summary>
        /// Sets a message handler for all validated clients. When set null, clients will buffer messages until a valid handler is set again.
        /// </summary>
        public MessageHandler? MessageHandler {
            get {
                return _msgHandler;
            }
            set {
                foreach (var c in validatedConnections)
                {
                    c.MessageHandler = value;
                }
                _msgHandler = value;
            }
        }

        /// <summary>
        /// Indicates whether or not this server has already been shut down.
        /// </summary>
        public bool Closed { get; private set; } = false;
        #endregion

        #region Events
        /// <summary>
        /// Called whenever a new client has been successfully validated.
        /// </summary>
        public event ValidatedClientHandler? OnConnectionValidated;

        /// <summary>
        /// Called whenever a new client failed validation. Note that the client connection is subsequently closed.
        /// </summary>
        public event ValidatingClientFailedHandler? OnValidationFailed;

        /// <summary>
        /// Called whenever a validated client disconnects or the connection breaks for any other reason.
        /// </summary>
        public event ConnectionDisconnectedHandler? OnConnectionDisconnected;
        #endregion

        #region Event Handlers

        private void NegotiationFailed(ConnectionNegotiation n, ValidationFailedException ex)
        {
            ongoingNegotiations.Remove(n);
            Task.Run(() => { OnValidationFailed?.Invoke(n.Connection, this, ex); });
        }

        private void NegotiationSucceeded(ConnectionNegotiation n)
        {
            validatedConnections.Add(n.Connection);
            ongoingNegotiations.Remove(n);
            n.Connection.MessageHandler = _msgHandler;
            n.Connection.ConnectionDisconnected += OnDisconnect;
            Task.Run(() => { OnConnectionValidated?.Invoke(n.Connection, this); });
        }

        protected virtual void OnDisconnect(Connection c, Exception? ex)
        {
            validatedConnections.Remove(c);
            Task.Run(() => { OnConnectionDisconnected?.Invoke(c, ex); });
        }

        private void NewConnection(Connection conn, ConnectionSource source) {

            if (Closed)
            {
                conn.CloseConnection();
                throw new ObjectDisposedException("Server already closed!");
            }
            Debug.WriteLine("New connection, starting negotiation!");
            ongoingNegotiations.Add(new(conn, ConnectionNegotiationPosition.Authorizer, Validators, NegotiationFailed, NegotiationSucceeded));
        }

        #endregion

        #region Virtual

        /// <summary>
        /// This will disconnect a validated client from the server, closing the connection.
        /// </summary>
        /// <param name="c"></param>
        public virtual void DisconnectClient(Connection c)
        {
            if (!validatedConnections.Contains(c)) throw new ArgumentException("This client is not connected to this server, or not yet validated.");
            validatedConnections.Remove(c);
            c.CloseConnection();
        }

        #endregion

        #region Closing & Disposal

        /// <summary>
        /// If closed, the server will stop accepting new connections, close all the already established ones and shut down all connection sources.
        /// </summary>
        public void Close()
        {
            if (Closed) return;
            foreach (var s in connectionSources)
            {
                s.Close();
            }
            foreach (var n in new List<ConnectionNegotiation>(ongoingNegotiations))
            {
                n.Dispose();
            }
            foreach (var c in new List<Connection>(validatedConnections))
            {
                c.Dispose();
            }
            ongoingNegotiations.Clear();
            validatedConnections.Clear();
            connectionSources.Clear();
            Closed = true;
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
