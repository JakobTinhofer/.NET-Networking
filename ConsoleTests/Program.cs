using NetworkTests;
using LightBlueFox.Networking;
using System.Diagnostics;

Console.WriteLine("Trying to establish connections...");
(NetworkConnection sender, NetworkConnection receiver) = ConnectionTest.GetTcpConnections(5556);
Console.WriteLine("Connections established.");
(receiver as TcpConnection ?? throw new Exception()).KeepMessagesInOrder = false;
int bs = 0;
var d = ConnectionTest.GenerateRandomData(250, 5000, out bs);
Console.WriteLine("Generated data.");
TaskCompletionSource<List<byte>[]> tcs = new TaskCompletionSource<List<byte>[]>();

List<List<byte>> receivedPackets = new List<List<byte>>();
int i = 0;
receiver.PacketHandler = (e, sender) =>
{
    Console.WriteLine(i + "r");
    i++;
    receivedPackets.Add(e.ToArray().ToList());
    if (receivedPackets.Count == d.Length) tcs.SetResult(receivedPackets.ToArray());
};
Stopwatch s = new Stopwatch();
Thread.Sleep(400);
s.Start();
foreach (var item in d)
{
    sender.WritePacket(item.ToArray());
}
Console.WriteLine("All packets sent.");
var res = tcs.Task.GetAwaiter().GetResult();
s.Stop();
Console.WriteLine("Finished.");
string b_s = "";
double secs = s.ElapsedMilliseconds * 1.024;
if (bs / (secs) > 1024)
    b_s += String.Format("{0:0.00} MB/s", (bs / 1024 ) / (secs));
else
    b_s += String.Format("{0:0.00} KB/s", bs / (secs));
Console.WriteLine(ConnectionTest.CompareL(res, d) + " after {0} ms. That is {1} or {2:0.00} Packets/s", s.ElapsedMilliseconds, b_s, ((((double)d.Length) * 1000) / ((double)s.ElapsedMilliseconds)));

void WriteArray(byte[] b)
{
    //Console.WriteLine(BitConverter.ToString(b));
}



bool CompareL(List<byte>[] e1, List<byte>[] e2)
{
    if (e1.Length != e2.Length) return false;
    bool res = true;
    for (int i = 0; i < e1.Length; i++)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        if (!ConnectionTest.Compare(e1[i].ToArray(), e2[i].ToArray()))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            res = false;
        }

        Console.Write("RECV: "); WriteArray(e1[i].ToArray());
        Console.Write("SEND: "); WriteArray(e2[i].ToArray());
        Console.WriteLine();
    }
    return res;
}