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
        private const string Users = @"D:\temp\messenger\Users";
        private const string BansPath = @"D:\temp\messenger\bans";
        private const string NicknamesAndPasswordsPath = @"D:\temp\messenger\nicknamesAndPasswords\users.json";
        private const string BansNicknames = @"D:\temp\messenger\bans\nicknamesBun.json";
        private const string BansIP = @"D:\temp\messenger\bans\IPsBun.json";
        private UserNicknameAndPasswordAndIPs user;
        private UserDeleter userDeleter;
        public async Task BanUser()
        {
            while (true)
            {
                Console.WriteLine("If you want to ban the user by the nickname, press n\n\r" +
                    "If you want to ban the user by IP, press i\n\r" +
                    "If you want to unban the IP, press u\n\r" +
                    "If you want to stop the server, press s\n\r" +
                    "If you want to delete all except the port, press d\n\r" +
                    "If you want to delete user, click h\n\r");
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
                    case ConsoleKey.W:
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
            var foundNick = await FindNeedNick();
            if (foundNick)
            {
                lock (messenger.locketOnline)
                {
                    foreach (var user in messenger.online)
                    {
                        if (user.Nickname == this.user.Nickname)
                        {
                            user.communication.EndTask = true;
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
            StopUsers();
            DeleteDirectories();
            server.CreateDirectories();
            StartServer();
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
        private void StopServer()
        {
            StopUsers();
            StartServer();
        }
        private void StartServer()
        {
            server.Connect = true;
            server.Run();
        }
        private void StopUsers()
        {
            server.Connect = false;
            lock (messenger.locketOnline)
            {
                foreach (var user in messenger.online)
                {
                    user.communication.EndTask = true;
                }
            }
            Console.WriteLine("Click something to start the server");
            Console.ReadKey(true);
        }
        private async Task Unban()
        {
            Console.WriteLine("Write name IP");
            var IP = Console.ReadLine();
            await fileMaster.ReadWrite(BansIP, (banIPs) =>
            {
                var haveIP = false;
                foreach (var banIP in banIPs)
                {
                    if (banIP == IP)
                    {
                        haveIP = true;
                        break;
                    }
                }
                if (haveIP)
                {
                    banIPs.Remove(IP);
                    return (banIPs, true);
                }
                else
                {
                    Console.WriteLine("Don`t have this IP in ban list");
                }
                return (banIPs, false);
            });
        }
        private async Task Baning(ConsoleKeyInfo key)
        {
            //////////////////////can delete file in user, users and leave users in group
            var foundNick = await FindNeedNick();
            if (foundNick) /////////////////////////////////can be mistake
            {
                if (await CheckBans())
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
            var banUsers = await fileMaster.ReadAndDesToLString(BansNicknames);
            if (banUsers == null)
            {
                return true;
            }
            foreach (var banUser in banUsers)
            {
                if (user.Nickname == banUser)
                {
                    return false;
                }
            }
            return true;
        }
        private async Task<bool> FindNeedNick()
        {
            Console.WriteLine("Write nickname");
            var line = Console.ReadLine();
            return await CheckNickname(line);
        }
        private async void BanOnIP()
        {
            BanOnNickname();
            await fileMaster.ReadWrite(BansIP, (banIPs) =>
            {
                if (banIPs == null)
                {
                    banIPs = new List<string>();
                }
                foreach (var userIP in user.IPs)
                {
                    banIPs.Add(userIP);
                }
                return (banIPs, true);
            });
        }
        private async void BanOnNickname()
        {
            lock (messenger.locketOnline)
            {
                foreach (var user in messenger.online)
                {
                    if (user.Nickname == this.user.Nickname)
                    {
                        user.communication.EndTask = true;
                    }
                }
            }
            await userDeleter.Run(user.Nickname, true);

            ////var userGroups = await ReadData($@"{Users}\{user.Nickname}\userGroups.json");
            ////var leavedUserGroups = await ReadData($@"{Users}\{user.Nickname}\leavedUserGroups.json");
            ////DeleteData(PublicGroupsPath, userGroups, leavedUserGroups);
            ////await DeleteNickInGroups(userGroups, PublicGroupsPath, "users.json");
            ////await DeleteNickInGroups(leavedUserGroups, PublicGroupsPath, "leavedPeople.json");
            //await DeleteData(PublicGroupsPath, $@"{Users}\{user.Nickname}\userGroups.json", $@"{Users}\{user.Nickname}\leavedUserGroups.json");

            ////var secretGroups = await ReadData($@"{Users}\{user.Nickname}\secretGroups.json");
            ////var leavedSecretGroups = await ReadData($@"{Users}\{user.Nickname}\leavedSecretGroups.json");
            ////DeleteData(SecretGroupsPath, secretGroups, leavedSecretGroups);
            ////await DeleteNickInGroups(secretGroups, SecretGroupsPath, "users.json");
            ////await DeleteNickInGroups(secretGroups, SecretGroupsPath, "leavedPeople.json");
            //await DeleteData(SecretGroupsPath, $@"{Users}\{user.Nickname}\secretGroups.json", $@"{Users}\{user.Nickname}\leavedSecretGroups.json");

            ////var peopleChatsBeen = await ReadData($@"{Users}\{user.Nickname}\peopleChatsBeen.json");
            ////var leavedPeopleChatsBeen = await ReadData($@"{Users}\{user.Nickname}\leavedPeopleChatsBeen.json");
            ////DeleteData(PeopleChatsPath, peopleChatsBeen, leavedPeopleChatsBeen);
            ////await DeleteNickInGroups(peopleChatsBeen, PeopleChatsPath, "users.json");
            ////await DeleteNickInGroups(leavedPeopleChatsBeen, PeopleChatsPath, "leavedPeople.json");
            //await DeleteData(PeopleChatsPath, $@"{Users}\{user.Nickname}\peopleChatsBeen.json", $@"{Users}\{user.Nickname}\leavedPeopleChatsBeen.json");

            //await DeleteInvitations();

            await fileMaster.ReadWrite(BansNicknames, (banUsers) =>
            {
                if (banUsers == null)
                {
                    banUsers = new List<string>();
                }
                banUsers.Add(user.Nickname);
                return (banUsers, true);
            });
            //Directory.Delete($"{Users}\\{user.Nickname}", true);
        }
        //private async Task DeleteInvitations()
        //{
        //    var invitations = await fileMaster.ReadAndDesToLString($@"{Users}\{user.Nickname}\invitation.json");
        //    if (invitations != null)
        //    {
        //        foreach (var invitation in invitations)
        //        {
        //            string path;
        //            switch (invitation[0])
        //            {
        //                case 'p':
        //                    path = $@"{PublicGroupsPath}\{invitation.Remove(0, 8)}\invitation.json";
        //                    break;
        //                case 's':
        //                    path = $@"{SecretGroupsPath}\{invitation.Remove(0, 8)}\invitation.json";
        //                    break;
        //                default:
        //                    path = "";
        //                    break;
        //            }
        //            await fileMaster.ReadWrite(path, (users) =>
        //            {
        //                users.Remove(user.Nickname);
        //                return (users, true);
        //            });
        //        }
        //    }
        //}
        //private async Task DeleteData(string pathTypeGroup, string pathUseGroups, string pathLeavedGroups)
        //{
        //    var groups = await fileMaster.ReadAndDesToLString(pathUseGroups);
        //    var leavedGroups = await fileMaster.ReadAndDesToLString(pathLeavedGroups);
        //    await ChangeMessages(pathTypeGroup, groups, leavedGroups);
        //    await DeleteNickInGroups(groups, pathTypeGroup, "users.json");
        //    await DeleteNickInGroups(leavedGroups, pathTypeGroup, "leavedPeople.json");
        //}
        //private async Task DeleteNickInGroups(List<string> groupNames, string firstPartOfThePast, string lastPartOfThePast)
        //{
        //    if (groupNames != null)
        //    {
        //        foreach (var groupName in groupNames)
        //        {
        //            var path = $"{firstPartOfThePast}\\{groupName}\\{lastPartOfThePast}";
        //            await fileMaster.ReadWrite(path, (users) => 
        //            { 
        //                users.Remove(user.Nickname);
        //                return (users, true);
        //            });
        //        }
        //    }
        //}
        //private async Task ChangeMessages(string path, List<string> firstGroups, List<string> secondGroups)
        //{
        //    await ChangeMessages(path, firstGroups);
        //    await ChangeMessages(path, secondGroups);
        //    //if (firstGroups != null)
        //    //{
        //    //    var firstGroupsPaths = firstGroups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
        //    //    foreach (var firstGroupPath in firstGroupsPaths)
        //    //    {
        //    //        await ReadWriteData(firstGroupPath);
        //    //    }
        //    //}
        //    //if (secondGroups != null)
        //    //{
        //    //    var SecondGroupsPaths = secondGroups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
        //    //    foreach (var SecondGroupPath in SecondGroupsPaths)
        //    //    {
        //    //        await ReadWriteData(SecondGroupPath);
        //    //    }
        //    //}
        //}
        //private async Task ChangeMessages(string path, List<string> groups)
        //{
        //    if (groups != null)
        //    {
        //        var SecondGroupsPaths = groups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
        //        foreach (var SecondGroupPath in groups)
        //        {
        //            await ReadWriteData(SecondGroupPath);
        //        }
        //    }
        //}

        ////private List<string> ReadGroups(string path)
        ////{
        ////    var groupsPaths = Directory.GetDirectories(path);
        ////    var groups = new List<string>();
        ////    foreach (var groupPath in groupsPaths)
        ////    {
        ////        groups.Add(Path.GetFileName(groupPath));
        ////    }
        ////    return groups;
        ////}
        //private async Task ReadWriteData(string path)
        //{
        //    //var messages = await fileMaster.ReadAndDesToLString(path);
        //    await fileMaster.ReadWrite(path, (messages) =>
        //    {
        //        var newMessages = new List<string>();
        //        if (messages != null)
        //        {
        //            foreach (var message in messages)
        //            {
        //                if (user.Nickname == message.Remove(user.Nickname.Length))
        //                {
        //                    newMessages.Add($"{user.Nickname} was banned");
        //                    continue;
        //                }
        //                newMessages.Add(message);
        //            }
        //        }
        //        return (newMessages, true);
        //    });
        //}
        private async Task<bool> CheckNickname(string nickname)
        {
            return await fileMaster.ReadWrite(NicknamesAndPasswordsPath, users =>
            {
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
                }
                return (users, true);
            });
            //var users = await fileMaster.ReadAndDesToLUserInf(NicknamesAndPasswordsPath);
            //var findUser = false;
            //foreach (var user in users)
            //{
            //    if (nickname == user.Nickname)
            //    {
            //        findUser = true;
            //        this.user = user;
            //        break;
            //    }
            //}
            //if (findUser)
            //{
            //    users.Remove(user);
            //}
            //return findUser;
        }
    }
}
