using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class BanerUsers
    {
        public BanerUsers(Messenger messenger, Server server)
        {
            this.messenger = messenger;
            this.server = server;
            fileMaster = new FileMaster();
            userDeleter = new UserDeleter(fileMaster);
        }
        private FileMaster fileMaster;
        private Server server;
        private Messenger messenger;
        private const string PublicGroupsPath = @"D:\temp\messenger\publicGroup";
        private const string SecretGroupsPath = @"D:\temp\messenger\secretGroup";
        private const string PeopleChatsPath = @"D:\temp\messenger\peopleChats";
        private const string UsersPath = @"D:\temp\messenger\Users";
        private const string BansPath = @"D:\temp\messenger\bans";
        private const string NicknamesAndPasswordsPath = @"D:\temp\messenger\nicknamesAndPasswords\users.json";
        private const string BansNicknamesPath = @"D:\temp\messenger\bans\nicknamesBun.json";
        private const string BansIPPath = @"D:\temp\messenger\bans\IPsBun.json";
        private UserNicknameAndPasswordAndIPs user;
        private UserDeleter userDeleter;
        public async Task BanUser()
        {
            while (true)
            {
                Console.WriteLine("If you want to ban the user by the nickname, press 'n'\n\r" +
                    "If you want to ban the user by IP, press 'i'\n\r" +
                    "If you want to unban the IP, press 'u'\n\r" +
                    "If you want to stop the server, press 's'\n\r" +
                    "If you want to start the server, press 'r'\n\r" +
                    "If you want to delete everything except ports, press 'd'\n\r" +
                    "If you want to delete user, press 'h'\n\r");
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.N:
                        await Baning(key);
                        break;
                    case ConsoleKey.I:
                        await Baning(key);
                        break;
                    case ConsoleKey.U:
                        await Unban();
                        break;
                    case ConsoleKey.S:
                        StopServer();
                        break;
                    case ConsoleKey.R:
                        StartServer();
                        break;
                    case ConsoleKey.D:
                        DeleteAll();
                        break;
                    case ConsoleKey.H:
                        await DeleteUser();
                        break;
                    default:
                        break;
                }

                //if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.I)
                //{
                //    await Baning(key);
                //}
                //else if (key.Key == ConsoleKey.U)
                //{
                //    await Unban();
                //}
                //else if (key.Key == ConsoleKey.S)
                //{
                //    StopServer();
                //}
                //else if (key.Key == ConsoleKey.W)
                //{
                //    DeleteAll();
                //}
            }
        }
        private async Task DeleteUser()
        {
            if (await FindNeedNick())
            {
                lock (messenger.locketOnline)
                {
                    foreach (var userOnline in messenger.online)
                    {
                        if (userOnline.Nickname == user.Nickname)
                        {
                            userOnline.communication.EndTask = true;
                            break;
                        }
                    }
                }
                await userDeleter.Run(user.Nickname, false);
            }
            else
            {
                Console.WriteLine("Don`t have this nickname");
            }
        }
        private void DeleteAll()
        {
            Console.WriteLine("If you really want to delete everything except ports, press 'Enter'");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                StopServer();
                DeleteDirectories();
                server.CreateDirectories();
                StartServer();
            }
        }
        private void DeleteDirectories()
        {
            fileMaster.DeleterFolder(@"D:\temp\messenger\bans");
            fileMaster.DeleterFolder(@"D:\temp\messenger\nicknamesAndPasswords");
            fileMaster.DeleterFolder(@"D:\temp\messenger\peopleChats");
            fileMaster.DeleterFolder(@"D:\temp\messenger\publicGroup");
            fileMaster.DeleterFolder(@"D:\temp\messenger\secretGroup");
            fileMaster.DeleterFolder(@"D:\temp\messenger\Users");
        }
        private void StartServer()
        {
            server.Connect = true;
            server.Run();
        }
        private void StopServer()
        {
            server.Connect = false;
            lock (messenger.locketOnline)
            {
                foreach (var user in messenger.online)
                {
                    user.communication.EndTask = true;
                }
            }
        }
        private async Task Unban()
        {
            Console.WriteLine("Write name IP");
            var IP = Console.ReadLine();
            await fileMaster.ReadWrite<string>(BansIPPath, banIPs =>
            {
                if ((banIPs ?? new List<string>()).Contains(IP))
                {
                    Console.WriteLine("Okey");
                    banIPs.Remove(IP);
                    return (banIPs, true);
                }
                else
                {
                    Console.WriteLine("Don`t have this IP in ban list");
                    return (banIPs, false);
                }
            });
        }
        private async Task Baning(ConsoleKeyInfo key)
        {
            var foundNick = await FindNeedNick();
            if (foundNick)
            {
                if (!await CheckBans())
                {
                    if (key.Key == ConsoleKey.N)
                    {
                        BanOnNickname();
                    }
                    else if (key.Key == ConsoleKey.I)
                    {
                        BanOnIP();
                    }
                }
                else
                {
                    Console.WriteLine("This nickname is in ban");
                }
            }
            else
            {
                Console.WriteLine("Don`t have this nickname");
            }
        }
        private async Task<bool> CheckBans()
        {
            var banUsers = await fileMaster.ReadAndDeserialize<string>(BansNicknamesPath);
            if (banUsers == null)
            {
                return false;
            }
            return banUsers.Contains(user.Nickname);
        }
        private async Task<bool> FindNeedNick()
        {
            Console.WriteLine("Write nickname");
            var line = Console.ReadLine();
            return await CheckAndRemoveNickname(line);
        }
        private async void BanOnIP()
        {
            BanOnNickname();
            await fileMaster.ReadWrite(BansIPPath, fileMaster.AddSomeData(user.IPs));
        }
        private async void BanOnNickname()
        {
            await fileMaster.ReadWrite(BansNicknamesPath, fileMaster.AddData(user.Nickname));

            lock (messenger.locketOnline)
            {
                foreach (var userOnline in messenger.online)
                {
                    if (userOnline.Nickname == user.Nickname)
                    {
                        userOnline.communication.EndTask = true;
                        break;
                    }
                }
            }
            await userDeleter.Run(user.Nickname, true);
        }
        private async Task<bool> CheckAndRemoveNickname(string nickname)
        {
            return await fileMaster.ReadWrite<UserNicknameAndPasswordAndIPs>(NicknamesAndPasswordsPath, users =>
            {
                if (users == null)
                {
                    return (users, false);
                }
                var findUser = false;
                foreach (var user in users)
                {
                    if (nickname == user.Nickname)
                    {
                        findUser = true;
                        this.user = user;
                        break;
                    }
                }
                if (findUser)
                {
                    users.Remove(user);
                    return (users, true);
                }
                return (users, false);
            });
        }
    }
}
