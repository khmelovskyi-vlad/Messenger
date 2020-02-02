using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class Server
    {
        public Server()
        {
        }
        public async Task Run(int countListener)
        {
            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var tcpEndPoint = new IPEndPoint(IPAddress.Any, port);
            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(5);
            Run();
            BanerUsers banerUsers = new BanerUsers();
            await banerUsers.BanUser();
        }
        private const int port = 1234;
        private Socket tcpSocket;
        private Messenger messenger = new Messenger();
        private async void Run()
        {
            await Task.Run(()=> tcpSocket.BeginAccept(async ar =>
            {
                try
                {
                    var listener = tcpSocket.EndAccept(ar);
                    Task.Run(() => Run());
                    var succesConnection = await messenger.Connect(listener);

                }
                catch (Exception socketException)
                {
                    throw socketException;
                }
            }, tcpSocket));
        }
    }
}
