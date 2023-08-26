namespace LightBlueFox.Connect.Structure.ConnectionSources
{
    public delegate void NewConnectionHandler(Connection con, ConnectionSource source);
    public abstract class ConnectionSource
    {
        public NewConnectionHandler? OnNewConnection;
    }
}
