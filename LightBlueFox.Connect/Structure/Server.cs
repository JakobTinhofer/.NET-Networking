using LightBlueFox.Connect.Structure.ConnectionSources;
using LightBlueFox.Connect.Structure.Validators;
using LightBlueFox.Connect.Util;
using System.Diagnostics;

namespace LightBlueFox.Connect.Structure
{
    public delegate void ValidatedClientHandler(Connection c, Server s);
    public delegate void ValidatingClientFailedHandler(Connection c, Server s, ValidationFailedException ex);
    public class Server : IDisposable
    {
        private List<ConnectionSource> connectionSources = new List<ConnectionSource>();
        private List<Connection> validatedConnections = new List<Connection>();
        private List<ConnectionNegotiation> ongoingNegotiations = new();

        #region Public Members
        public IReadOnlyCollection<Connection> Connections { get
            {
                return validatedConnections.AsReadOnly();
            } 
        }

        public ConnectionValidator[] Validators;
        public IReadOnlyCollection<ConnectionSource> ConnectionSources
        {
            get
            {
                return connectionSources.AsReadOnly();
            }
        }

        

        private MessageHandler? _msgHandler = null;
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

        #endregion

        #region Events
        public event ValidatedClientHandler? OnConnectionValidated;
        public event ValidatingClientFailedHandler? OnValidationFailed;
        public event ConnectionDisconnectedHandler? OnConnectionDisconnected;
        #endregion

        #region Callbacks

        private void negotiationFailed(ConnectionNegotiation n, ValidationFailedException ex)
        {
            ongoingNegotiations.Remove(n);
            Task.Run(() => { OnValidationFailed?.Invoke(n.Connection, this, ex); });
        }

        private void negotiationSucceeded(ConnectionNegotiation n)
        {
            validatedConnections.Add(n.Connection);
            ongoingNegotiations.Remove(n);
            n.Connection.MessageHandler = _msgHandler;
            n.Connection.ConnectionDisconnected += OnDisconnect;
            Task.Run(() => { OnConnectionValidated?.Invoke(n.Connection, this); });
        }

        private void newConnection(Connection conn, ConnectionSource source) {

            if (Closed)
            {
                conn.CloseConnection();
                throw new ObjectDisposedException("Server already closed!");
            }
            Debug.WriteLine("New connection, starting negotiation!");
            ongoingNegotiations.Add(new(conn, ConnectionNegotiationPosition.Authorizer, Validators, negotiationFailed, negotiationSucceeded));
        }

        #endregion

        #region Virtual

        public virtual void DisconnectClient(Connection c)
        {
            validatedConnections.Remove(c);
            c.CloseConnection();
        }

        protected virtual void OnDisconnect(Connection c, Exception? ex)
        {
            validatedConnections.Remove(c);
            Task.Run(() => { OnConnectionDisconnected?.Invoke(c, ex); });
        }
        
        #endregion

        

        public Server(ConnectionValidator[] validators, params ConnectionSource[] sources)
        {
            Validators = validators;
            foreach (ConnectionSource source in sources)
            {
                source.OnNewConnection = newConnection;
                connectionSources.Add(source);
            }
        }

        public Server(ConnectionValidator validator, params ConnectionSource[] sources) : this(new ConnectionValidator[1] {validator}, sources) { }


        public bool Closed { get; private set; } = false;


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
        }
    }
}
