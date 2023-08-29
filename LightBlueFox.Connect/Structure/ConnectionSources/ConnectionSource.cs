namespace LightBlueFox.Connect.Structure.ConnectionSources
{
    /// <summary>
    /// Defines a handler for the <see cref="ConnectionSource.OnNewConnection"/> event that is called when a nwe connection has been established.
    /// </summary>
    /// <param name="con">The newly established connection.</param>
    /// <param name="source">The source of the connection.</param>
    public delegate void NewConnectionHandler(Connection con, ConnectionSource source);
    
    /// <summary>
    /// This class provides an abstract and unified structure for establishing various type of connections.
    /// For example, in the case of a TcpSource, the concrete implementation might manage a listening socket accepting new connections.
    /// </summary>
    public abstract class ConnectionSource
    {
        /// <summary>
        /// Called for every successfully established connection.
        /// </summary>
        public NewConnectionHandler? OnNewConnection;

        /// <summary>
        /// Stop the source from establishing any new connections.
        /// </summary>
        public abstract void Close();
    }
}
