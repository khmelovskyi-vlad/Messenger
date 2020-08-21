using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net; //////////////////////////only for IP, need this?
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    class Connector
    {
        public Connector(Socket socket, Messenger messenger)
        {
            this.listener = socket;
            IP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            communication = new Communication(socket);
            this.messenger = messenger;
        }
        Messenger messenger;
        private string IP { get; }
        private Socket listener { get; }
        private Communication communication;
        private string FilePath { get { return Path.Combine(messenger.Server.NicknamesAndPasswordsPath,"users.json"); } }
        private string nickname = "";
        private async Task<bool> CheckNicknamesBan(string nick)
        {
            var nicksBan = await FileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.BansPath, "nicknamesBun.json"));
            if (nicksBan != null)
            {
                foreach (var nickBan in nicksBan)
                {
                    if (nick == nickBan)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private async Task<bool> CheckIPsBan()
        {
            var path = Path.Combine(messenger.Server.BansPath, "IPsBun.json");
            var IPsBan = await FileMaster.ReadAndDeserialize<string>(path);
            if (IPsBan != null)
            {
                foreach (var IPBan in IPsBan)
                {
                    if (IP == IPBan)
                    {
                        await communication.SendMessage("Your IP is in ban, bye");
                        return true;
                    }
                }
            }
            return false;
        }
        public async Task<string> Run()
        {
            if (await CheckIPsBan())
            {
                return "?Disconnect";
            }
            IEnumerable<UserNicknameAndPasswordAndIPs> usersData = await TakeAllUserData();
            if (await SelectMode(usersData))
            {
                return nickname;
            }
            else
            {
                return "";
            }
        }
        private async Task<bool> SelectMode(IEnumerable<UserNicknameAndPasswordAndIPs> usersData)
        {
            var successConnection = false;
            switch (await communication.SendMessageAndAnswerClient($"If you want to connect to the server using your account, press Enter,{Environment.NewLine}" +
                $"if you want to create a new account, press Tab,{Environment.NewLine}" +
                $"if you want to log out, click Escape,{Environment.NewLine}" +
                $"if you want to delete your account, click Delete,{Environment.NewLine}" +
                $"if you want to exit the messenger, write '??' at any moment{Environment.NewLine}"))
            {
                case "using":
                    successConnection = await ConnectRegisteredUser(usersData);
                    break;
                case "new":
                    successConnection = await CreateNewAccount(usersData);
                    break;
                case "escape":
                    successConnection = await DisconectUser(usersData);
                    break;
                case "delete":
                    successConnection = await DeleteAccount(usersData);
                    break;
                default:
                    successConnection = await SelectMode(usersData);
                    break;
            }
            return successConnection;
        }
        private async Task<bool> DisconectUser(IEnumerable<UserNicknameAndPasswordAndIPs> usersData)
        {
            await communication.SendMessage("You realy want to disconect? If yes click Enter");
            await communication.AnswerClient();
            if (communication.data.ToString() == "Yes")
            {
                await communication.SendMessage("Ok, bye");
                return false;
            }
            await communication.SendMessage("Сhoose what you want to do");
            await communication.AnswerClient();
            return await SelectMode(usersData);
        }
        private async Task<bool> DeleteAccount(IEnumerable<UserNicknameAndPasswordAndIPs> usersData)
        {
            var userNicknameAndPassword = await CheckNicknameAndPassword(usersData);
            if (userNicknameAndPassword.Equals(default(UserNicknameAndPasswordAndIPs)))
            {
                return false;
            }
            await communication.SendMessage("Do you realy want, delete your accaunt? if yes, click Enter");
            await communication.AnswerClient();
            if (communication.data.ToString() != "Yes")
            {
                return false;
            }
            var result = await FileMaster.UpdateFile<UserNicknameAndPasswordAndIPs>(FilePath, oldData =>
            {
                if (oldData == null)
                {
                    return (oldData, false);
                }
                else
                {
                    if (!LastFindData(oldData, userNicknameAndPassword.Nickname))
                    {
                        return (oldData, false);
                    }
                    oldData = oldData.Where(x => x.Nickname != userNicknameAndPassword.Nickname).ToList();
                }
                return (oldData, true);
            });
            if (result)
            {
                await communication.SendMessage("Account was deleted");
                UserDeleter userDeleter = new UserDeleter(messenger);
                await userDeleter.Run(userNicknameAndPassword.Nickname, false);
            }
            else
            {
                await communication.SendMessage("Don`t have this nickname");
            }
            return false;
        }
        private bool LastFindData(IEnumerable<UserNicknameAndPasswordAndIPs> usersData, string nick)
        {
            foreach (var userData in usersData)
            {
                if (nick == userData.Nickname)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> CreateNewAccount(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            await communication.SendMessage("Enter a nickname");
            while (true)
            {
                var findNick = false;
                var nick = await communication.AnswerClient();
                if (userData != null)
                {
                    foreach (var userNicknameAndPassword in userData)
                    {
                        if (nick == userNicknameAndPassword.Nickname)
                        {
                            findNick = true;
                            await communication.SendMessage("This nickname is currently in use, enter new please");
                            break;
                        }
                    }
                }
                if (!findNick)
                {
                    return await EnterPasswordAndSaveData(nick);
                }
            }
        }
        private async Task<bool> EnterPasswordAndSaveData(string nick)
        {
            nick = await CheckNickname(nick);
            if (nick.Length == 0)
            {
                return false;
            }
            var password = await CheckPassword(new UserNicknameAndPasswordAndIPs(), false);
            if (password == "")
            {
                return false;
            }
            else
            {
                return await AddNewUser(new string[] { nick, password });
            }
        }
        private async Task<string> CheckNickname(string nick)
        {
            var lastString = "";
            for (int i = 10; i > 0; i--)
            {
                lastString = $"{Environment.NewLine}a number of attempts  left = {i - 1}";
                if (nick.Length <= 5)
                {
                    await communication.SendMessage(CreateMessage($"Enter nickname bigger than 5 symbols {lastString}", i));
                }
                else
                {
                    var goodInput = CharacterCheckers.CheckInput(nick);
                    if (goodInput)
                    {
                        if (!await CheckNicknamesBan(nick))
                        {
                            if (!CheckNicknamesOnline(nick))
                            {
                                return nick;
                            }
                            else
                            {
                                await communication.SendMessage(CreateMessage($"This nickname is online, write else {lastString}", i));
                            }
                        }
                        else
                        {
                            await communication.SendMessage(CreateMessage($"This nickname is in ban, write else {lastString}", i));
                        }
                    }
                    else
                    {
                        await communication.SendMessage(CreateMessage($"The nickname can only contain lowercase letters and numbers {lastString}", i));
                    }
                }
                if (i != 1)
                {
                    nick = await communication.AnswerClient();
                }
            }
            return "";
        }
        private string CreateMessage(string message, int i)
        {
            if (i == 1)
            {
                return "Number of attempts 0, so bye";
            }
            else
            {
                return message;
            }
        }
        private bool CheckNicknamesOnline(string nick)
        {
            lock (messenger.OnlineLock)
            {
                return messenger.online.Select(user => user.Nickname).Contains(nick);
            }
        }
        private async Task<bool> CheckWantingToConnect()
        {
            if (await communication.AnswerClient() == "Enter")
            {
                return true;
            }
            return false;
        }
        private async Task<UserNicknameAndPasswordAndIPs> CheckNicknameAndPassword(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            var nick = await communication.SendMessageAndAnswerClient("Enter a nickname");
            if (userData == null)
            {
                await communication.SendMessage("Don`t have your data");
                return default(UserNicknameAndPasswordAndIPs);
            }
            while (true)
            {
                nick = await CheckNickname(nick);
                if (nick.Length == 0)
                {
                    return default(UserNicknameAndPasswordAndIPs);
                }
                foreach (var userNicknameAndPassword in userData)
                {
                    if (nick == userNicknameAndPassword.Nickname)
                    {
                        if (await CheckPassword(userNicknameAndPassword, true) != "")
                        {
                            await communication.SendMessage("You enter to messenger");
                            nickname = userNicknameAndPassword.Nickname;
                            return userNicknameAndPassword;
                        }
                        else
                        {
                            return default(UserNicknameAndPasswordAndIPs);
                        }
                    }
                }
                nick = await communication.SendMessageAndAnswerClient("Wrong nickname, enter new");
            }
        }
        private async Task<string> CheckPassword(UserNicknameAndPasswordAndIPs userNicknameAndPassword, bool checkHavingPassword)
        {
            await communication.SendMessage("Enter password bigger than 7 symbols");
            for (int i = 5; i > 0; i--)
            {
                var password = await communication.AnswerClient();
                if (await CheckPasswordCondition(userNicknameAndPassword, checkHavingPassword, password, i))
                {
                    return password;
                }
            }
            return "";
        }
        private async Task<bool> CheckPasswordCondition(UserNicknameAndPasswordAndIPs userNicknameAndPassword, bool checkHavingPassword, string password, int i)
        {
            if (password.Length < 8)
            {
                if (i == 1)
                {
                    await communication.SendMessage("Number of attempts 0, so bye");
                }
                else
                {
                    await communication.SendMessage($"Password length < 8, enter another password, a number of attempts left = {i - 1}");
                }
                return false;
            }
            if (checkHavingPassword)
            {
                if (password != userNicknameAndPassword.Password)
                {
                    if (i == 1)
                    {
                        await communication.SendMessage($"Number of attempts 0, so bye");
                    }
                    else
                    {
                        await communication.SendMessage($"Wrong password, a number of attempts left = {i - 1}");
                    }
                    return false;
                }
            }
            return true;
        }
        private async Task<bool> ConnectRegisteredUser(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            var userNicknameAndPassword = await CheckNicknameAndPassword(userData);
            if (userNicknameAndPassword.Equals(default(UserNicknameAndPasswordAndIPs)))
            {
                return false;
            }
            await AddIP(userNicknameAndPassword);
            return true;
        }
        private async Task AddIP(UserNicknameAndPasswordAndIPs userNicknameAndPasswordAndIPs)
        {
            if (!userNicknameAndPasswordAndIPs.IPs.Contains(IP))
            {
                await FileMaster.UpdateFile<UserNicknameAndPasswordAndIPs>(FilePath, users =>
                {
                    var userWithNewIP = users
                    .Where(acc => acc.Nickname == userNicknameAndPasswordAndIPs.Nickname && acc.Password == userNicknameAndPasswordAndIPs.Password)
                    .First();
                    users.Remove(userWithNewIP);
                    userWithNewIP.IPs.Add(IP);
                    users.Add(userWithNewIP);
                    return (users, true);
                });
            }
        }
        private async Task<IEnumerable<UserNicknameAndPasswordAndIPs>> TakeAllUserData()
        {
            return await FileMaster.ReadAndDeserialize<UserNicknameAndPasswordAndIPs>(FilePath);
        }
        private async Task<bool> AddNewUser(IEnumerable<string> userData)
        {
            var IPs = new List<string>();
            IPs.Add(IP);
            UserNicknameAndPasswordAndIPs userNicknameAndPassword = new UserNicknameAndPasswordAndIPs(userData.First(), userData.Last(), IPs);
            var result = await FileMaster.UpdateFile<UserNicknameAndPasswordAndIPs>(FilePath, oldData =>
            {
                if (oldData == null)
                {
                    oldData = new List<UserNicknameAndPasswordAndIPs>();
                }
                else
                {
                    if (!LastCheck(oldData, userNicknameAndPassword.Nickname))
                    {
                        return (oldData, false);
                    }
                }
                oldData.Add(userNicknameAndPassword);
                return (oldData, true);
            });
            if (result)
            {
                await communication.SendMessage("You enter to messenger");
                FileMaster.CreateDirectory(Path.Combine(messenger.Server.UsersPath, userNicknameAndPassword.Nickname));
                nickname = userNicknameAndPassword.Nickname;
            }
            else
            {
                await communication.SendMessage("This nickname is currently in use, bye");
            }
            return result;
        }
        private bool LastCheck(IEnumerable<UserNicknameAndPasswordAndIPs> usersData, string nick)
        {
            foreach (var userData in usersData)
            {
                if (nick == userData.Nickname)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
