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
        //private void Test(Socket socket)
        //{
        //    var path = @"D:\temp\k.mp4";
        //    //var path = @"D:\temp\ok2";
        //    var s = File.Exists(path);
        //    Console.WriteLine(s);
        //    if (s)
        //    {
        //        socket.SendFile(path);
        //        Console.ReadKey();
        //    }
        //    Console.ReadKey();
        //}
        //private void Test(Socket listener)
        //{
        //    var s = "IMG_20191203_201515.jpg";
        //    //var firstMessage = Encoding.ASCII.GetBytes(@"D:\temp\ok\IMG_20191203_201515.jpg");
        //    //var lastMessage = Encoding.ASCII.GetBytes("Thanks");
        //    //listener.SendFile(@"D:\temp\ok2\IMG_20191203_201515.jpg", firstMessage, lastMessage, TransmitFileOptions.ReuseSocket);
        //    listener.Send(Encoding.ASCII.GetBytes("IMG_20191203_201515.jpg"));
        //    var buffer = new byte[256];
        //    listener.Receive(buffer);
        //    listener.SendFile($@"D:\temp\ok2\{s}");
        //}
    }
}
