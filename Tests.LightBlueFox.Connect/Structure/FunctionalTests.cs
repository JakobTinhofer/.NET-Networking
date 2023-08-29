using LightBlueFox.Connect;
using LightBlueFox.Connect.Structure;
using LightBlueFox.Connect.Net.ConnectionSources;
using LightBlueFox.Connect.Structure.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LightBlueFox.Connect.Net;
using System.Threading.Tasks;
using System.Numerics;
using System.Net.Sockets;
using System.Net;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace Tests.LightBlueFox.Connect.Structure
{
    [TestClass]
    public class FunctionalTests
    {
        [TestMethod]
        public void TestServerConnectSingleClient()
        {
            DateTime t = DateTime.UtcNow;
            Server myServer;
            Connection myClientConnection;

            TaskCompletionSource<bool> serverMessageMatches = new();
            TaskCompletionSource<bool> clientMessageMatches = new();

            Task badTask = Task.Run(() => Task.Delay(10000));

            Task goodTask = Task.Run<bool>(() => {
                myServer = new(new NameValidator("test123"), new TcpSource(12321))
                {
                    MessageHandler = (b, a) =>
                    {
                        serverMessageMatches.SetResult(b.Length == 1 && b[0] == 123);
                        a.Sender.WriteMessage(new byte[1] { 222 });
                    }
                };
                Debug.WriteLine("Time passed: " + (DateTime.UtcNow - t).ToString(@"ss\.ff"));
                myClientConnection = ConnectionNegotiation.ValidateConnection(new TcpConnection("localhost", 12321), ConnectionNegotiationPosition.Challenger, new NameValidator("test123"));
                myClientConnection.MessageHandler = (b, a) => {
                    clientMessageMatches.SetResult(b.Length == 1 && b[0] == 222);
                };

#pragma warning disable IDE0230 // Use UTF-8 string literal
                myClientConnection.WriteMessage(new byte[1] { 123 });
#pragma warning restore IDE0230 // Use UTF-8 string literal
                return serverMessageMatches.Task.GetAwaiter().GetResult() && clientMessageMatches.Task.GetAwaiter().GetResult();
            });

            Assert.IsTrue(Task.WaitAny(badTask, goodTask) == 1, "The negotiation was aborted since the task timed out.");
            Assert.IsTrue(((Task<bool>)goodTask).GetAwaiter().GetResult(), "The message contents were not as expected.");
        }

        [TestMethod]
        public void TestServerFailClient()
        {
            TaskCompletionSource<bool> didValidate = new();

            Server s = new(new NameValidator("1234"), new TcpSource(44444, IPAddress.Loopback));
            
            s.OnValidationFailed += (c, s, ex) => { didValidate.SetResult(false); };
            s.OnConnectionValidated += (c, s) => { didValidate.SetResult(true); };

            Assert.ThrowsException<ValidationFailedException>(() => ConnectionNegotiation.ValidateConnection(new TcpConnection(IPAddress.Loopback.ToString(), 44444), ConnectionNegotiationPosition.Challenger, new NameValidator("4321")));

            Assert.IsFalse(didValidate.Task.GetAwaiter().GetResult());
        }

        [DataTestMethod]
        [DataRow(4, 34, 8)]
        public void TestSpecialServerClients(int clientsExpected, int clientPcktsExpected, int serverPcktsExpected)
        {
            TaskCompletionSource finishedAllClientMessages = new(false);
            Random r = new();
            int serverPcktsSent = 0;
            int clientPcktsSent = 0;
            int clientPcktsReceived = 0;
            int serverPcktsReceived = 0;


            int clientsConnected = 0;
            int clientsDisconnected = 0;

            byte[] messageBytes = { 11, 22, 33, 44, 55 };
            byte[] reverseMessageBytes = messageBytes.Reverse().ToArray();

            Debug.WriteLine("Conns: {0}, ClientPackets: {1}, ServerPackets: {2}", clientsExpected, clientPcktsExpected, serverPcktsExpected);

            Server s = new(new NameValidator("1234321"), new TcpSource(12321, IPAddress.Loopback));

            s.OnConnectionDisconnected += (c, s) => {
                clientsDisconnected++;
                Debug.WriteLine("[CLIENT DISCONNECT] client disconnected. CPR/SPR: {0}/{2}, CPR/CPS: {1}{3}, CD/CC: {5}/{4}", clientPcktsReceived, clientPcktsSent, serverPcktsReceived, serverPcktsSent, clientsConnected, clientsDisconnected);
            };

            s.MessageHandler = (m, args) =>
            {
                if (m.SequenceEqual(reverseMessageBytes)) serverPcktsReceived++;
                else throw new Exception("Received invalid data from client!");
                if (serverPcktsReceived == clientPcktsExpected * clientsExpected && clientPcktsReceived == serverPcktsExpected * clientsExpected && !finishedAllClientMessages.Task.IsCompleted) finishedAllClientMessages.SetResult();
                Debug.WriteLine("[SERVER HANDLER] received packet. CPR: {0}, CPS: {1}, SPR: {2}, SPS: {3}, CC: {4}, CD: {5}", clientPcktsReceived, clientPcktsSent, serverPcktsReceived, serverPcktsSent, clientsConnected, clientsDisconnected);
            };

            s.OnConnectionValidated += (c, s) =>
            {
                Debug.WriteLine("[CLIENT VALIDATED] validated client. CPR: {0}, CPS: {1}, SPR: {2}, SPS: {3}, CC: {4}, CD: {5}", clientPcktsReceived, clientPcktsSent, serverPcktsReceived, serverPcktsSent, clientsConnected, clientsDisconnected);
                for (int i = 0; i < serverPcktsExpected; i++)
                {
                    Thread.Sleep(r.Next(0, 100));
                    c.WriteMessage(messageBytes);
                    serverPcktsSent++;

                }
            };
            List<Connection> conns = new();
            for (int i = 0; i < clientsExpected; i++)
            {
                Task.Run(() => {
                    clientsConnected++;
                    Connection c = ConnectionNegotiation.ValidateConnection(new TcpConnection(IPAddress.Loopback.ToString(), 12321), ConnectionNegotiationPosition.Challenger, new NameValidator("1234321"));
                    conns.Add(c);
                    c.MessageHandler = (m, args) =>
                    {
                        if (m.SequenceEqual(messageBytes)) clientPcktsReceived++;
                        else throw new Exception("Received invalid data from server!");
                        if (serverPcktsReceived == clientPcktsExpected * clientsExpected && clientPcktsReceived == serverPcktsExpected * clientsExpected && !finishedAllClientMessages.Task.IsCompleted) finishedAllClientMessages.SetResult();
                        Debug.WriteLine("[CLIENT HANDLER] received packet. CPR: {0}, CPS: {1}, SPR: {2}, SPS: {3}, CC: {4}, CD: {5}", clientPcktsReceived, clientPcktsSent, serverPcktsReceived, serverPcktsSent, clientsConnected, clientsDisconnected);
                    };
                    for (int j = 0; j < clientPcktsExpected; j++)
                    {
                        Thread.Sleep(r.Next(0, 100));
                        c.WriteMessage(reverseMessageBytes);
                        clientPcktsSent++;
                    }

                });
            }

            Assert.IsTrue(finishedAllClientMessages.Task.Wait(20000), "Timed out");

            foreach (var c in conns)
            {
                c.Dispose();
            }

            s.Dispose();
        }

        [TestMethod]
        public void TestServerConnectMultipleClientRandom()
        {
            Random r = new();
            TestSpecialServerClients(r.Next(2, 5), r.Next(5, 50), r.Next(5, 50));
        }


    }
}
