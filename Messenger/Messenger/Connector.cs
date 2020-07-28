using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public Connector(Socket listener, Messenger messenger)
        {
            this.listener = listener;
            IP = ((IPEndPoint)listener.RemoteEndPoint).Address.ToString();
            communication = new Communication(listener);
            this.messenger = messenger;
        }
        Messenger messenger;
        FileMaster fileMaster = new FileMaster();
        private string IP { get; }
        private Socket listener { get; }
        private Communication communication;
        private string FilePath { get { return @"D:\temp\messenger\nicknamesAndPasswords\users.json"; } }
        private string UserFoldersPath { get { return @"D:\temp\messenger\Users"; } }
        private string nickname = "";
        private async Task<bool> CheckNicknamesBan(string nick)
        {
            var nicksBan = await fileMaster.ReadAndDeserialize<string>(@"D:\temp\messenger\bans\nicknamesBun.json");
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
            var IPsBan = await fileMaster.ReadAndDeserialize<string>(@"D:\temp\messenger\bans\IPsBun.json");
            if (IPsBan != null)
            {
                foreach (var IPBan in IPsBan)
                {
                    if (IP == IPBan)
                    {
                        communication.SendMessage("Your IP is in ban, bye");
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
            communication.SendMessage("If you want to connect to the server using your account, press Enter,\n\r" +
                "if you want to create a new account, press Tab,\n\r" +
                "if you want to log out, click Escape,\n\r" +
                "if you want to delete your account, click Delete,\n\r" +
                "if you want to exit the messenger, write '??' at any moment\n\r");
            communication.AnswerClient();
            var successConnection = false;
            switch (communication.data.ToString())
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
            communication.SendMessage("You realy want to disconect? If yes click Enter");
            communication.AnswerClient();
            if (communication.data.ToString() == "Yes")
            {
                communication.SendMessage("Ok, bye");
                return false;
            }
            communication.SendMessage("Сhoose what you want to do");
            communication.AnswerClient();
            return await SelectMode(usersData);
        }
        private async Task<bool> DeleteAccount(IEnumerable<UserNicknameAndPasswordAndIPs> usersData)
        {
            var userNicknameAndPassword = await CheckNicknameAndPassword(usersData);
            if (userNicknameAndPassword.Equals(default(UserNicknameAndPasswordAndIPs)))
            {
                return false;
            }
            communication.SendMessage("Do you realy want, delete your accaunt? if yes, click Enter");
            communication.AnswerClient();
            if (communication.data.ToString() != "Yes")
            {
                return false;
            }
            var result = await fileMaster.ReadWrite<UserNicknameAndPasswordAndIPs>(FilePath, oldData =>
            {
                if (oldData == null)
                {
                    communication.SendMessage("Don`t have your data");
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
                communication.SendMessage("Account was deleted");
                UserDeleter userDeleter = new UserDeleter(fileMaster);
                await userDeleter.Run(userNicknameAndPassword.Nickname, false);
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
            communication.SendMessage("Don`t have this nickname");
            return false;
        }
        private async Task<bool> CreateNewAccount(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            communication.SendMessage("Enter a nickname");
            while (true)
            {
                var findNick = false;
                communication.AnswerClient();
                if (userData != null)
                {
                    foreach (var userNicknameAndPassword in userData)
                    {
                        if (communication.data.ToString() == userNicknameAndPassword.Nickname)
                        {
                            findNick = true;
                            communication.SendMessage("This nickname is currently in use, enter new please");
                            break;
                        }
                    }
                }
                if (!findNick)
                {
                    return await EnterPasswordAndSaveData();
                }
            }
        }
        private async Task<bool> EnterPasswordAndSaveData()
        {
            while (true)
            {
                var nick = await CheckNickname();
                if (nick.Length == 0)
                {
                    return false;
                }
                var password = CheckPassword(new UserNicknameAndPasswordAndIPs(), false);
                if (password == "")
                {
                    if (!CheckWantingToConnect())
                    {
                        return false;
                    }
                }
                else
                {
                    return await AddNewUser(new string[] { nick, password });
                }
            }
        }
        private async Task<string> CheckNickname()
        {
            while (true)
            {
                var lastString = "";
                for (int i = 0; i < 10; i++)
                {
                    if (i == 9)
                    {
                        lastString = "\n\rYou really want to conect to the server, if yes - click Enter";
                    }
                    var foundSymdol = false;
                    if (communication.data.Length <= 5)
                    {
                        communication.SendMessage($"Enter nickname bigger than 5 symbols {lastString}");
                    }
                    else
                    {
                        foreach (var symbol in communication.data.ToString())
                        {
                            if (symbol == '\\' || symbol == '/' || symbol == ':' || symbol == '*' || symbol == '?'
                                || symbol == '"' || symbol == '<' || symbol == '>' || symbol == '|')
                            {
                                foundSymdol = true;
                                var invertedComma = '"';
                                communication.SendMessage($"nickname cannot contain characters such as:\n\r' ', '\\', '/'," +
                                    $" ':', '*', '?', '{invertedComma}', '<', '>', '|' {lastString}");
                                break;
                            }
                        }
                        if(!foundSymdol)
                        {
                            if (!await CheckNicknamesBan(communication.data.ToString()))
                            {
                                if (!CheckNicknamesOnline(communication.data.ToString()))
                                {
                                    return communication.data.ToString();
                                }
                                communication.SendMessage($"This nickname is online, write else {lastString}");
                            }
                            communication.SendMessage($"This nickname is in ban, write else {lastString}");
                        }
                    }
                    communication.AnswerClient();
                }
                if (!CheckWantingToConnect())
                {
                    return "";
                }
            }
        }
        private bool CheckNicknamesOnline(string nick)
        {
            lock (messenger.locketOnline)
            {
                return messenger.online.Select(user => user.Nickname).Contains(nick);
            }
        }
        private bool CheckWantingToConnect()
        {
            if (communication.data.ToString() == "Enter")
            {
                return true;
            }
            return false;
        }
        private async Task<UserNicknameAndPasswordAndIPs> CheckNicknameAndPassword(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            communication.SendMessage("Enter a nickname");
            communication.AnswerClient();
            if (userData == null)
            {
                communication.SendMessage("Don`t have your data");
                return default(UserNicknameAndPasswordAndIPs);
            }
            while (true)
            {
                var findNick = false;
                var nick = await CheckNickname();
                if (nick.Length == 0)
                {
                    return default(UserNicknameAndPasswordAndIPs);
                }
                foreach (var userNicknameAndPassword in userData)
                {
                    if (nick == userNicknameAndPassword.Nickname)
                    {
                        findNick = true;
                        if (CheckPassword(userNicknameAndPassword, true) != "")
                        {
                            communication.SendMessage("You enter to messenger");
                            nickname = userNicknameAndPassword.Nickname;
                            return userNicknameAndPassword;
                        }
                        else
                        {
                            if (!CheckWantingToConnect())
                            {
                                return default(UserNicknameAndPasswordAndIPs);
                            }
                        }
                    }
                }
                if (!findNick)
                {
                    communication.SendMessage("Wrong nickname, enter new");
                    communication.AnswerClient();
                }
            }
        }
        private string CheckPassword(UserNicknameAndPasswordAndIPs userNicknameAndPassword, bool checkHavingPassword)
        {
            communication.SendMessage("Enter password bigger than 7 symbols");
            var numberAttemps = 5;
            for (int i = 0; i < numberAttemps; i++)
            {
                communication.AnswerClient();
                if (CheckPasswordCondition(userNicknameAndPassword, checkHavingPassword))
                {
                    return communication.data.ToString();
                }
                else
                {
                    if (i == 4)
                    {
                        communication.SendMessage($"Password length < 7,\n\r" +
                            $"You really want to conect to the server, if yes - click Enter");
                        communication.AnswerClient();
                    }
                    else
                    {
                        communication.SendMessage($"Password length < 7, enter another password, a number of attemps left = {numberAttemps - i - 1}");
                    }
                }
            }
            return "";
        }
        private bool CheckPasswordCondition(UserNicknameAndPasswordAndIPs userNicknameAndPassword, bool checkHavingPassword)
        {
            if (checkHavingPassword)
            {
                return communication.data.ToString() == userNicknameAndPassword.Password;
            }
            else
            {
                return communication.data.Length > 7;
            }
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
                await fileMaster.ReadWrite<UserNicknameAndPasswordAndIPs>(FilePath, users =>
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
            return await fileMaster.ReadAndDeserialize<UserNicknameAndPasswordAndIPs>(FilePath);
        }
        private async Task<bool> AddNewUser(IEnumerable<string> userData)
        {
            var IPs = new List<string>();
            IPs.Add(IP);
            UserNicknameAndPasswordAndIPs userNicknameAndPassword = new UserNicknameAndPasswordAndIPs(userData.First(), userData.Last(), IPs);
            var result = await fileMaster.ReadWrite<UserNicknameAndPasswordAndIPs>(FilePath, oldData =>
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
                communication.SendMessage("You enter to messenger");
                fileMaster.CreateDirectory($"{UserFoldersPath}\\{userNicknameAndPassword.Nickname}");
                nickname = userNicknameAndPassword.Nickname;
            }
            return result;
        }
        private bool LastCheck(IEnumerable<UserNicknameAndPasswordAndIPs> usersData, string nick)
        {
            foreach (var userData in usersData)
            {
                if (nick == userData.Nickname)
                {
                    communication.SendMessage("This nickname is currently in use, bye");
                    return false;
                }
            }
            return true;
        }
    }
}
