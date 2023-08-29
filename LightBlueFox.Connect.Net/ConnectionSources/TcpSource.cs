using LightBlueFox.Connect.Structure.ConnectionSources;
using LightBlueFox.Connect.Net;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics.Contracts;
using System;

namespace LightBlueFox.Connect.Net.ConnectionSources
{
    /// <summary>
    /// Manages a socket that listens for incoming connections.
    /// </summary>
    public class TcpSource : ConnectionSource, IDisposable
    {
        #region Constructors
        /// <summary>
        /// Creates a new source from an existing and already bound socket.
        /// </summary>
        /// <param name="listener">The existing socket. It must already be bound to a local endpoint.</param>
        /// <exception cref="ArgumentException">Thrown when the socket is not bound.</exception>
        /// <exception cref="NullReferenceException">Thrown when the local endpoint of the socket is null.</exception>
        /// /// <exception cref="ProtocolViolationException">Thrown when the socket is not of type <see cref="ProtocolType.Tcp"/>.</exception>
        public TcpSource(Socket listener)
        {
            sock = listener;

            if (!sock.IsBound) throw new ArgumentException("Given socket was not yet bound.");
            if (sock.LocalEndPoint == null) throw new NullReferenceException("Local endpoint on socket is null!");
            if (sock.ProtocolType != ProtocolType.Tcp) throw new ProtocolViolationException("The TcpSource only supports tcp sockets.");
            LocalEndpoint = (IPEndPoint)sock.LocalEndPoint;

            sock.Listen(((IPEndPoint)sock.LocalEndPoint).Port);
            sock.BeginAccept(new AsyncCallback(AcceptClient), null);
        }

        /// <summary>
        /// Creates a new source that listens on a given ip and port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="address">The interface to listen on. Defaults to <see cref="IPAddress.Any"/></param>
        public TcpSource(int port, IPAddress? address = null)
        {
            sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new(address ?? IPAddress.Any, port);
            sock.Bind(ep);
            LocalEndpoint = ep;
            sock.Listen(port);
            sock.BeginAccept(new AsyncCallback(AcceptClient), null);
        }
        #endregion

        #region Fields
        /// <summary>
        /// The endpoint where the source listens for new connections.
        /// </summary>
        public readonly IPEndPoint LocalEndpoint;

        private readonly Socket sock;
        #endregion

        #region Accepting Clients

        private void AcceptClient(IAsyncResult ar)
        {
            try
            {
                var client = sock.EndAccept(ar);
                Task.Run(() => { OnNewConnection?.Invoke(new TcpConnection(client), this); });
                sock.BeginAccept(new AsyncCallback(AcceptClient), null);
            }
            catch (Exception ex) {
                if (!(ex is SocketException || ex is ObjectDisposedException)) throw;
            } 
        }
        #endregion

        #region Closing & Disposing
        /// <summary>
        /// Close the source and stop accepting clients.
        /// </summary>
        public override void Close()
        {
            sock.Close();
            sock.Dispose();
        }

        public void Dispose()
        {
            sock.Close();
            ((IDisposable)sock).Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
