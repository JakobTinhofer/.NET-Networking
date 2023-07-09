namespace LightBlueFox.Networking
{
    // This describes any connection between two programs, be it over the internet or whatever other communication media.
    public abstract class Connection
    {
        /// <summary>
        /// A method which handles any incomming packets
        /// </summary>
        public PacketHandler? PacketHandler { get; set; }
        
        /// <summary>
        /// Gets called when the connection is terminated. An exception is provided as the reason for the disconnection.
        /// </summary>
        public event ConnectionDisconnectedHandler? ConnectionDisconnected;

        /// <summary>
        /// Sends a packet to the program at the other side.
        /// </summary>
        /// <param name="Packet">Just the packet data, no size prefix required.</param>
        public abstract void WritePacket(ReadOnlyMemory<byte> Packet);

        public abstract void CloseConnection();

        protected void CallConnectionClosed(Exception? ex)
        {
            Task.Run(() => ConnectionDisconnected?.Invoke(this, ex));
        }

        
    }

    /// <summary>
    /// Describes a method equipped to handle packets coming from an active <see cref="Connection"/>.
    /// </summary>
    /// <param name="sender">The connection which received the packet.</param>
    /// <param name="packet">The raw data that was received.</param>
    public delegate void PacketHandler(ReadOnlySpan<byte> packet, PacketArgs args);

    /// <summary>
    /// Describes methods which can be used to listen to the <see cref="Connection.ConnectionDisconnected"/> event.
    /// </summary>
    /// <param name="sender">The connection that was terminated.</param>
    /// <param name="disconnectionException">The reason for the termination. Null if the connection terminated as planned.</param>
    public delegate void ConnectionDisconnectedHandler(Connection sender, Exception? disconnectionException);
}