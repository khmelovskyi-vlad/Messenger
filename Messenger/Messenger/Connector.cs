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
        public Connector(Socket socket, Messenger messenger, FileMaster fileMaster)
        {
            this.listener = socket;
            IP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            communication = new Communication(socket);
            this.messenger = messenger;
            this.fileMaster = fileMaster;
        }
        Messenger messenger;
        FileMaster fileMaster;
        private string IP { get; }
        private Socket listener { get; }
        private Communication communication;
        private string FilePath { get { return Path.Combine(messenger.Server.NicknamesAndPasswordsPath,"users.json"); } }
        private string nickname = "";
        private async Task<bool> CheckNicknamesBan(string nick)
        {
            var nicksBan = await fileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.BansPath, "nicknamesBun.json"));
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
            var IPsBan = await fileMaster.ReadAndDeserialize<string>(path);
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
            await communication.SendMessage($"If you want to connect to the server using your account, press Enter,{Environment.NewLine}" +
                $"if you want to create a new account, press Tab,{Environment.NewLine}" +
                $"if you want to log out, click Escape,{Environment.NewLine}" +
                $"if you want to delete your account, click Delete,{Environment.NewLine}" +
                $"if you want to exit the messenger, write '??' at any moment{Environment.NewLine}");
            await communication.AnswerClient();
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
            var result = await fileMaster.UpdateFile<UserNicknameAndPasswordAndIPs>(FilePath, oldData =>
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
                UserDeleter userDeleter = new UserDeleter(fileMaster, messenger);
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
                await communication.AnswerClient();
                if (userData != null)
                {
                    foreach (var userNicknameAndPassword in userData)
                    {
                        if (communication.data.ToString() == userNicknameAndPassword.Nickname)
                        {
                            findNick = true;
                            await communication.SendMessage("This nickname is currently in use, enter new please");
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
                var password = await CheckPassword(new UserNicknameAndPasswordAndIPs(), false);
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
                        lastString = $"{Environment.NewLine}You really want to conect to the server, if yes - click Enter";
                    }
                    if (communication.data.Length <= 5)
                    {
                        await communication.SendMessage($"Enter nickname bigger than 5 symbols {lastString}");
                    }
                    else
                    {
                        var goodInput = CharacterCheckers.CheckInput(communication.data.ToString());
                        if(goodInput)
                        {
                            if (!await CheckNicknamesBan(communication.data.ToString()))
                            {
                                if (!CheckNicknamesOnline(communication.data.ToString()))
                                {
                                    return communication.data.ToString();
                                }
                                else
                                {
                                    await communication.SendMessage($"This nickname is online, write else {lastString}");
                                }
                            }
                            else
                            {
                                await communication.SendMessage($"This nickname is in ban, write else {lastString}");
                            }
                        }
                        else
                        {
                            await communication.SendMessage($"The nickname can only contain lowercase letters and numbers {lastString}");
                        }
                    }
                    await communication.AnswerClient();
                }
                if (!CheckWantingToConnect())
                {
                    return "";
                }
            }
        }
        private bool CheckNicknamesOnline(string nick)
        {
            lock (messenger.OnlineLock)
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
            await communication.SendMessage("Enter a nickname");
            await communication.AnswerClient();
            if (userData == null)
            {
                await communication.SendMessage("Don`t have your data");
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
                        if (await CheckPassword(userNicknameAndPassword, true) != "")
                        {
                            await communication.SendMessage("You enter to messenger");
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
                    await communication.SendMessage("Wrong nickname, enter new");
                    await communication.AnswerClient();
                }
            }
        }
        private async Task<string> CheckPassword(UserNicknameAndPasswordAndIPs userNicknameAndPassword, bool checkHavingPassword)
        {
            await communication.SendMessage("Enter password bigger than 7 symbols");
            var numberAttemps = 5;
            for (int i = 0; i < numberAttemps; i++)
            {
                await communication.AnswerClient();
                if (CheckPasswordCondition(userNicknameAndPassword, checkHavingPassword))
                {
                    return communication.data.ToString();
                }
                else
                {
                    if (i == 4)
                    {
                        await communication.SendMessage($"Password length < 7,{Environment.NewLine}" +
                            $"You really want to conect to the server, if yes - click Enter");
                        await communication.AnswerClient();
                    }
                    else
                    {
                        await communication.SendMessage($"Password length < 7, enter another password, a number of attemps left = {numberAttemps - i - 1}");
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
                await fileMaster.UpdateFile<UserNicknameAndPasswordAndIPs>(FilePath, users =>
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
            var result = await fileMaster.UpdateFile<UserNicknameAndPasswordAndIPs>(FilePath, oldData =>
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
                fileMaster.CreateDirectory(Path.Combine(messenger.Server.UsersPath, userNicknameAndPassword.Nickname));
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
