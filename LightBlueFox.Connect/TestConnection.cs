namespace LightBlueFox.Connect
{
    /// <summary>
    /// A simple connection created in pairs that instantly transmit data from one twin to the other. Used for testing.
    /// </summary>
    public class TestConnection : Connection
    {
        public TestConnection? Partner;

        public static (TestConnection, TestConnection) CreatePair()
        {
            var c1 = new TestConnection();
            var c2 = new TestConnection();
            c1.Partner = c2;
            c2.Partner = c1;
            return (c1, c2);
        }

        public override void CloseConnection() { }

        public override void WriteMessage(ReadOnlyMemory<byte> Packet)
        {
            if (Partner != null) Partner.recMsg(Packet);
        }

        private void recMsg(ReadOnlyMemory<byte> data)
        {
            MessageReceived(data, new Util.MessageArgs(this), null);
        }
    }
}
