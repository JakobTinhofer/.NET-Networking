using NetworkTests;
using LightBlueFox.Networking;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;

Console.WriteLine("Hi there. This is how the library is used.");

TcpListener server = new TcpListener(IPAddress.Any, 33333);
server.Start();
Task<TcpConnection> acceptClient = Task<TcpConnection>.Run(() => {
    return new TcpConnection(server.AcceptSocket());
});
TcpConnection myConnection = new TcpConnection("127.0.0.1", 33333);
myConnection.MessageHandler = MessageHandler;

TcpConnection serverSideConnection = acceptClient.Result; 
serverSideConnection.WritePacket(Encoding.ASCII.GetBytes("Hello, world!"));

Console.ReadKey(true);

static void MessageHandler(ReadOnlySpan<byte> packet, MessageArgs args)
{
    Console.WriteLine("Received message of len {0} from server!", packet.Length);
    Console.WriteLine("Message: " + Encoding.ASCII.GetString(packet));
}