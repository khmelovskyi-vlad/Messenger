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
        public bool Connect = true;
        FileMaster fileMaster = new FileMaster();
        public async Task Run(int countListener)
        {
            CreateDirectories();
            await ReadWritePort();
            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var tcpEndPoint = new IPEndPoint(IPAddress.Any, port);
            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(5);
            while (true)
            {
                await Run();
                BanerUsers banerUsers = new BanerUsers(messenger, this);
                await banerUsers.BanUser();
            }
        }
        private async Task ReadWritePort()
        {
            await fileMaster.ReadWrite<int>(@"D:\temp\messenger\ports.json", ports =>
            {
                if (ports == null)
                {
                    ports = new List<int>();
                }
                Console.WriteLine("Ports:");
                foreach (var onePort in ports)
                {
                    Console.WriteLine(onePort);
                }
                while (true)
                {
                    Console.WriteLine("Write name port");
                    //var port = Console.ReadLine();
                    var port = "1234";
                    try
                    {
                        this.port = Convert.ToInt32(port);
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Bad input, try again");
                    }
                }
                Console.WriteLine("If you want to save port, click Enter");
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    foreach (var onePort in ports)
                    {
                        if (onePort == port)
                        {
                            return (ports, false);
                        }
                    }
                    ports.Add(port);
                    return (ports, true);
                }
                return (ports, false);
            });
        }
        private int port;
        private Socket tcpSocket;
        private Messenger messenger = new Messenger();
        public async Task Run()
        {
            await Task.Run(()=> tcpSocket.BeginAccept(async ar =>
            {
                try
                {
                    if (Connect)
                    {
                        Task.Run(() => Run());
                        using (var listener = tcpSocket.EndAccept(ar))
                        {
                            var succesConnection = await messenger.Connect(listener);
                        }
                    }

                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (Exception socketException)
                {
                    //throw socketException;
                }
            }, tcpSocket));
        }
        public void CreateDirectories()
        {
            fileMaster.CreateDirectory(@"D:\temp\messenger\bans");
            fileMaster.CreateDirectory(@"D:\temp\messenger\nicknamesAndPasswords");
            fileMaster.CreateDirectory(@"D:\temp\messenger\peopleChats");
            fileMaster.CreateDirectory(@"D:\temp\messenger\publicGroup");
            fileMaster.CreateDirectory(@"D:\temp\messenger\secretGroup");
            fileMaster.CreateDirectory(@"D:\temp\messenger\Users");
        }
    }
}
