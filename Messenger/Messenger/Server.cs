using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    class Server
    {
        public Server(string mainDirectoryPath)
        {
            this.mainDirectoryPath = mainDirectoryPath;
            messenger = new Messenger(this);
        }
        public string mainDirectoryPath { get; }
        public string BansPath { get { return Path.Combine(mainDirectoryPath, "bans"); } }
        public string NicknamesAndPasswordsPath { get { return Path.Combine(mainDirectoryPath, "nicknamesAndPasswords"); } }
        public string PeopleChatsPath { get { return Path.Combine(mainDirectoryPath, "peopleChats"); } }
        public string PublicGroupPath { get { return Path.Combine(mainDirectoryPath, "publicGroup"); } }
        public string SecretGroupPath { get { return Path.Combine(mainDirectoryPath, "secretGroup"); } }
        public string UsersPath { get { return Path.Combine(mainDirectoryPath, "Users"); } }
        public bool Connect = true;
        public async Task Run(int countListener)
        {
            CreateDirectories();
            await SetUpPort();
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var tcpEndPoint = new IPEndPoint(IPAddress.Any, port);
            listener.Bind(tcpEndPoint);
            listener.Listen(5);
            while (true)
            {
                Run();
                CommandInterpreter banerUsers = new CommandInterpreter(messenger, this);
                await banerUsers.Run();
            }
        }
        private int port;
        private Socket listener;
        private Messenger messenger;
        public async Task Run()
        {
            while (true)
            {
                var socket = await Task.Factory.FromAsync(listener.BeginAccept(null, null), listener.EndAccept);
                if (Connect)
                {
                    var succesConnection = messenger.Connect(socket);
                }
            }
            Run();
            //await Task.Run(() => listener.BeginAccept(async ar =>
            //{
            //    try
            //    {
            //        if (Connect)
            //        {
            //            Task.Run(() => Run());
            //            using (var socket = listener.EndAccept(ar))
            //            {
            //                var succesConnection = await messenger.Connect(socket);
            //            }
            //        }

            //    }
            //    catch (OperationCanceledException ex)
            //    {
            //        Console.WriteLine(ex);
            //    }
            //    catch (Exception socketException)
            //    {
            //        Console.WriteLine(socketException);
            //        //throw socketException;
            //    }
            //}, listener));
        }
        private async Task SetUpPort()
        {
            await FileMaster.UpdateFile<int>(Path.Combine(mainDirectoryPath, "mainDirectoryPath"), ports =>
            {
                if (ports == null)
                {
                    ports = new List<int>();
                }
                ShowPorts(ports);
                ReadPort(ports);
                return SavePort(ports);
            });
        }
        private (List<int>, bool) SavePort(List<int> ports)
        {
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
        }
        private void ShowPorts(List<int> ports)
        {
            Console.WriteLine("Ports:");
            foreach (var onePort in ports)
            {
                Console.WriteLine(onePort);
            }
        }
        private void ReadPort(List<int> ports)
        {
            while (true)
            {
                Console.WriteLine("Write name port");
                //var readedPort = Console.ReadLine();
                var readedPort = "1234";
                try
                {
                    port = Convert.ToInt32(readedPort);
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Bad input, try again");
                }
            }
        }
        public void CreateDirectories()
        {
            FileMaster.CreateDirectory(BansPath);
            FileMaster.CreateDirectory(NicknamesAndPasswordsPath);
            FileMaster.CreateDirectory(PeopleChatsPath);
            FileMaster.CreateDirectory(PublicGroupPath);
            FileMaster.CreateDirectory(SecretGroupPath);
            FileMaster.CreateDirectory(UsersPath);
        }
    }
}
