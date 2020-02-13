using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        }
        Server server;
        Messenger messenger;
        private const string PublicGroupsPath = @"D:\temp\messenger\publicGroup";
        private const string SecretGroupsPath = @"D:\temp\messenger\secretGroup";
        private const string PeopleChatsPath = @"D:\temp\messenger\peopleChats";
        private const string Users = @"D:\temp\messenger\Users";
        private const string BansPath = @"D:\temp\messenger\bans";
        private const string NicknamesAndPasswordsPath = @"D:\temp\messenger\nicknamesAndPasswords\users.json";
        private const string BansNicknames = @"D:\temp\messenger\bans\nicknamesBun.json";
        private const string BansIP = @"D:\temp\messenger\bans\IPsBun.json";
        private UserNicknameAndPasswordAndIPs user;
        public async Task BanUser()
        {
            while (true)
            {
                Console.WriteLine("If you want to ban the user by the nickname, press n\n\r" +
                    "If you want to ban the user by IP, press i\n\r" +
                    "If you want to unban the IP, press u\n\r" +
                    "If you want to stop the server, press s\n\r" +
                    "If you want to delete all except the port, press d\n\r");
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
        private void DeleteAll()
        {

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
            Console.WriteLine("Click something to start the server");
            Console.ReadKey(true);
            server.Connect = true;
            server.Run();
        }
        private async Task Unban()
        {
            Console.WriteLine("Write name IP");
            var IP = Console.ReadLine();
            var banIPs = await ReadData(BansIP);
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
                await WriteData(BansIP, banIPs);
            }
            else
            {
                Console.WriteLine("Don`t have this IP in ban list");
            }
        }
        private async Task Baning(ConsoleKeyInfo key)
        {
            //////////////////////can delete file in user, users and leave users in group
            user = await FindNeedNick();
            if (!user.Equals(new UserNicknameAndPasswordAndIPs())) /////////////////////////////////can be mistake
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
            var banUsers = await ReadData(BansNicknames);
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
        private async Task<UserNicknameAndPasswordAndIPs> FindNeedNick()
        {
            Console.WriteLine("Write nickname");
            var line = Console.ReadLine();
            return await CheckNickname(line);
        }
        private async void BanOnIP()
        {
            BanOnNickname();
            var banIPs = await ReadData(BansIP);
            if (banIPs == null)
            {
                banIPs = new List<string>();
            }
            foreach (var userIP in user.IPs)
            {
                banIPs.Add(userIP);
            }
            await WriteData(BansIP, banIPs);
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
            //var userGroups = await ReadData($@"{Users}\{user.Nickname}\userGroups.json");
            //var leavedUserGroups = await ReadData($@"{Users}\{user.Nickname}\leavedUserGroups.json");
            //DeleteData(PublicGroupsPath, userGroups, leavedUserGroups);
            //await DeleteNickInGroups(userGroups, PublicGroupsPath, "users.json");
            //await DeleteNickInGroups(leavedUserGroups, PublicGroupsPath, "leavedPeople.json");
            await DeleteDate(PublicGroupsPath, $@"{Users}\{user.Nickname}\userGroups.json", $@"{Users}\{user.Nickname}\leavedUserGroups.json");

            //var secretGroups = await ReadData($@"{Users}\{user.Nickname}\secretGroups.json");
            //var leavedSecretGroups = await ReadData($@"{Users}\{user.Nickname}\leavedSecretGroups.json");
            //DeleteData(SecretGroupsPath, secretGroups, leavedSecretGroups);
            //await DeleteNickInGroups(secretGroups, SecretGroupsPath, "users.json");
            //await DeleteNickInGroups(secretGroups, SecretGroupsPath, "leavedPeople.json");
            await DeleteDate(SecretGroupsPath, $@"{Users}\{user.Nickname}\secretGroups.json", $@"{Users}\{user.Nickname}\leavedSecretGroups.json");

            //var peopleChatsBeen = await ReadData($@"{Users}\{user.Nickname}\peopleChatsBeen.json");
            //var leavedPeopleChatsBeen = await ReadData($@"{Users}\{user.Nickname}\leavedPeopleChatsBeen.json");
            //DeleteData(PeopleChatsPath, peopleChatsBeen, leavedPeopleChatsBeen);
            //await DeleteNickInGroups(peopleChatsBeen, PeopleChatsPath, "users.json");
            //await DeleteNickInGroups(leavedPeopleChatsBeen, PeopleChatsPath, "leavedPeople.json");
            await DeleteDate(PeopleChatsPath, $@"{Users}\{user.Nickname}\peopleChatsBeen.json", $@"{Users}\{user.Nickname}\leavedPeopleChatsBeen.json");

            await DeleteInvitations();
            var banUsers = await ReadData(BansNicknames);
            if (banUsers == null)
            {
                banUsers = new List<string>();
                banUsers.Add(user.Nickname);
            }
            else
            {
                banUsers.Add(user.Nickname);
            }
            await WriteData(BansNicknames, banUsers);
            //Directory.Delete($"{Users}\\{user.Nickname}", true);
        }
        private async Task DeleteInvitations()
        {
            var invitations = await ReadData($@"{Users}\{user.Nickname}\invitation.json");
            List<string[]> data = new List<string[]>();
            foreach (var invitation in invitations)
            {
                string path;
                switch (invitation[0])
                {
                    case 'p':
                        path = $@"{PublicGroupsPath}\{invitation.Remove(0, 8)}\invitation.json";
                        break;
                    case 's':
                        path = $@"{SecretGroupsPath}\{invitation.Remove(0, 8)}\invitation.json";
                        break;
                    default:
                        path = "";
                        break;
                }
                var users = await ReadData(path);
                users.Remove(user.Nickname);
                await WriteData(path, users);
            }
        }
        private async Task DeleteDate(string pathTypeGroup, string pathUseGroups, string pathLeavedGroups)
        {
            var groups = await ReadData(pathUseGroups);
            var leavedGroups = await ReadData(pathLeavedGroups);
            DeleteData(pathTypeGroup, groups, leavedGroups);
            await DeleteNickInGroups(groups, pathTypeGroup, "users.json");
            await DeleteNickInGroups(leavedGroups, pathTypeGroup, "leavedPeople.json");
        }
        private async Task DeleteNickInGroups(List<string> groupNames, string firstPartOfThePast, string lastPartOfThePast)
        {
            if (groupNames != null)
            {
                foreach (var groupName in groupNames)
                {
                    var path = $"{firstPartOfThePast}\\{groupName}\\{lastPartOfThePast}";
                    var users = await ReadData(path);
                    users.Remove(user.Nickname);
                    await WriteData(path, users);
                }
            }
        }
        private async void DeleteData(string path, List<string> firstGroups, List<string> SecondGroups)
        {
            if (firstGroups != null)
            {
                var firstGroupsPaths = firstGroups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
                foreach (var firstGroupPath in firstGroupsPaths)
                {
                    await ReadWriteData(firstGroupPath);
                }
            }
            if (SecondGroups != null)
            {
                var SecondGroupsPaths = SecondGroups.Select(x => $@"{path}\{x}\data.json");
                foreach (var SecondGroupPath in SecondGroupsPaths)
                {
                    await ReadWriteData(SecondGroupPath);
                }
            }
        }

        //private List<string> ReadGroups(string path)
        //{
        //    var groupsPaths = Directory.GetDirectories(path);
        //    var groups = new List<string>();
        //    foreach (var groupPath in groupsPaths)
        //    {
        //        groups.Add(Path.GetFileName(groupPath));
        //    }
        //    return groups;
        //}
        private async Task ReadWriteData(string path)
        {
            var messages = await ReadData(path);
            if (messages == null)
            {
                return;
            }
            var newMessages = new List<string>();
            foreach (var message in messages)
            {
                if (user.Nickname == message.Remove(user.Nickname.Length))
                {
                    newMessages.Add($"{user.Nickname} was banned");
                    continue;
                }
                newMessages.Add(message);
            }
            await WriteData(path, newMessages);
        }
        private async Task WriteData(string path, List<string> data)
        {
            using (var stream = new StreamWriter(path, false))
            {
                var dataJson = JsonConvert.SerializeObject(data);
                await stream.WriteAsync(dataJson);
            }

        }
        private async Task<UserNicknameAndPasswordAndIPs> CheckNickname(string nickname)
        {
            var users = await ReadUsersData();
            foreach (var user in users)
            {
                if (nickname == user.Nickname)
                {
                    return user;
                }
            }
            return new UserNicknameAndPasswordAndIPs();
        }
        private async Task<List<string>> ReadData(string path)
        {
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                return JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
        }
        private async Task<List<UserNicknameAndPasswordAndIPs>> ReadUsersData()
        {
            using (var stream = File.Open(NicknamesAndPasswordsPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                return JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
            }
        }
        private async Task<StringBuilder> ReadFile(FileStream stream)
        {
            StringBuilder usersJson = new StringBuilder();
            var buffer = 256;
            var arrayBytes = new byte[buffer];
            while (true)
            {
                var readedRealBytes = await stream.ReadAsync(arrayBytes, 0, buffer);
                usersJson.Append(Encoding.Default.GetString(arrayBytes, 0, readedRealBytes));
                if (readedRealBytes < buffer)
                {
                    break;
                }
            }
            return usersJson;
        }
    }
}
