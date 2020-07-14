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
        public Connector(Socket listener)
        {
            this.listener = listener;
            IP = ((IPEndPoint)listener.RemoteEndPoint).Address.ToString();
            communication = new Communication(listener);
        }
        FileMaster fileMaster = new FileMaster();
        private string IP { get; }
        private Socket listener { get; }
        private Communication communication;
        private string FilePath { get { return @"D:\temp\messenger\nicknamesAndPasswords\users.json"; } }
        private string UserFoldersPath { get { return @"D:\temp\messenger\Users"; } }
        private string nickname = "";
        private async Task<bool> CheckNicknamesBan(string nick)
        {
            var nicksBan = await fileMaster.ReadAndDesToLString(@"D:\temp\messenger\bans\nicknamesBun.json");
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
            var IPsBan = await fileMaster.ReadAndDesToLString(@"D:\temp\messenger\bans\IPsBun.json");
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
        //public void CheckData()
        //{
        //    communication.AnswerClient(listener);
        //}
        //private void CreateFolderAndFile(UserNicknameAndPasswordAndIPs userNicknameAndPassword)
        //{
        //    fileMaster.CreateFolderAndFile($"{UserFoldersPath}\\{userNicknameAndPassword.Nickname}");
        //    //Directory.CreateDirectory($"{UserFoldersPath}\\{userNicknameAndPassword.Nickname}");
        //    //using (var stream = File.Open($@"{UserFoldersPath}\{userNicknameAndPassword.Nickname}\Data.json", FileMode.Create, FileAccess.Write))
        //    //{
        //    //    var arrayBytes = Encoding.Default.GetBytes(userNicknameAndPassword.Password);
        //    //    await stream.WriteAsync(arrayBytes, 0, arrayBytes.Length);
        //    //}
        //}
        //private void DeleterFolder(string nick)
        //{
        //    Directory.Delete($"{UserFoldersPath}\\{nick}", true);
        //}
        public async Task<string> Run()
        {
            if (await CheckIPsBan())
            {
                return "?Disconnect";
            }
            IEnumerable<UserNicknameAndPasswordAndIPs> usersData = await TakeAllUserData();
            var result = await WaitForSelectMode(usersData);
            return nickname;
        }
        private async Task<bool> WaitForSelectMode(IEnumerable<UserNicknameAndPasswordAndIPs> usersData)
        {
            communication.SendMessage("If you want to connect to the server using your nickname, press Enter,\n\r" +
                "if you want to create a new user, press Tab,\n\r" +
                "if you want to log out, click Escape,\n\r" +
                "if you want to delete your account, click Delete\n\r");
            communication.AnswerClient();
            var successConnection = false;
            switch (communication.data.ToString())
            {
                case "using":
                    successConnection = await ConnectUsedNickname(usersData);
                    break;
                case "new":
                    successConnection = await ConnectNewUser(usersData);
                    break;
                case "escape":
                    successConnection = await DisconectUser(usersData);
                    break;
                case "delete":
                    successConnection = await DeleterNickname(usersData);/////////////////////////// if don`t have nickname?
                    break;
                default:
                    successConnection = await WaitForSelectMode(usersData);
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
            return await WaitForSelectMode(usersData);
        }
        private async Task<bool> DeleterNickname(IEnumerable<UserNicknameAndPasswordAndIPs> usersData)
        {
            var userNicknameAndPassword = await CheckNicknameAndPassword(usersData, false);
            if (userNicknameAndPassword.Equals(default(UserNicknameAndPasswordAndIPs)))
            {
                return false;
            }
            StringBuilder usersJson;
            communication.SendMessage("Do you realy want, delete your accaunt? if yes, click Enter");
            communication.AnswerClient();
            if (communication.data.ToString() != "Yes")
            {
                return false;
            }
            var result = await fileMaster.ReadWrite(FilePath, oldData =>
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
                    oldData.Remove(userNicknameAndPassword);
                }
                return (oldData, true);
            });
            if (result)
            {
                communication.SendMessage("Index was deleter");
                UserDeleter userDeleter = new UserDeleter(fileMaster);
                await userDeleter.Run(userNicknameAndPassword.Nickname, false);
                //fileMaster.DeleterFolder($"{UserFoldersPath}\\{userNicknameAndPassword.Nickname}");
            }
            return result;
            //using (FileStream stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            //{
            //    usersJson = await AddFileToSB(stream);
            //    var oldData = JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
            //    if (oldData == null)
            //    {
            //        communication.SendMessage("Don`t have your data");
            //        return false;
            //    }
            //    else
            //    {
            //        if (!LastFindData(oldData, userNicknameAndPassword.Nickname))
            //        {
            //            return false;
            //        }
            //        oldData.Remove(userNicknameAndPassword);
            //        userDataJson = JsonConvert.SerializeObject(oldData);
            //    }
            //}
            //using (FileStream stream = File.Open(FilePath, FileMode.Truncate, FileAccess.Write))
            //{
            //    byte[] arrayBytes = Encoding.Default.GetBytes(userDataJson);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    stream.Write(arrayBytes, 0, arrayBytes.Length);
            //    communication.SendMessage("Index was deleter");
            //    fileMaster.DeleterFolder($"{UserFoldersPath}\\{userNicknameAndPassword.Nickname}");
            //    return true; //////////////////////////////////////////////////////////////////////think must be false
            //}
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
        private async Task<bool> ConnectNewUser(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            communication.SendMessage("Enter a nickname that does not begin with '?'");
            if (userData == null)
            {
                communication.AnswerClient();
                CheckDisconect(); ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var successSaveData = await EnterPasswordAndSaveData();
                if (!successSaveData)
                {
                    return await ConnectNewUserToOlds(userData);
                }
                return true;
            }
            return await ConnectNewUserToOlds(userData);
        }
        private async Task<bool> ConnectNewUserToOlds(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            while (true)
            {
                var findNick = false;
                communication.AnswerClient();
                foreach (var userNicknameAndPassword in userData)
                {
                    //if (data.ToString() == "?")
                    //{
                    //    return false;
                    //}
                    if (communication.data.ToString() == userNicknameAndPassword.Nickname)
                    {
                        findNick = true;
                        communication.SendMessage("This nickname is currently in use, enter new please");
                        break;
                    }
                }
                if (!findNick)
                {
                    var successSaveData = await EnterPasswordAndSaveData();
                    if (!successSaveData)
                    {
                        await ConnectNewUserToOlds(userData);
                        //await ConnectNewUserToOlds(userData);
                    }
                    return true;
                }
            }
        }
        private async Task<bool> EnterPasswordAndSaveData()
        {
            var nick = await CheckNickname();
            communication.SendMessage("Enter password bigger than 7 symbols");
            var password = CheckPassword();
            communication.AnswerClient();
            communication.SendMessage("LastCheck");
            communication.AnswerClient();
            return await AddNewUser(new string[] { nick, password });
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
                        lastString = "last check";
                    }
                    var findSymdol = false;
                    foreach (var symbol in communication.data.ToString())
                    {
                        if (symbol == '\\' || symbol == '/' || symbol == ':' || symbol == '*' || symbol == '?'
                            || symbol == '"' || symbol == '<' || symbol == '>' || symbol == '|')
                        {
                            findSymdol = true;
                            var invertedComma = '"';
                            communication.SendMessage($"nickname cannot contain characters such as:\n\r' ', '\\', '/', ':', '*', '?', '{invertedComma}', '<', '>', '|' {lastString}");
                            break;
                        }
                    }
                    if (!findSymdol)
                    {
                        if (communication.data.Length <= 5)
                        {
                            communication.SendMessage($"Enter nickname bigger than 5 symbols {lastString}");
                        }
                        else
                        {
                            //if (await CheckNicknamesBan(communication.data.ToString()))
                            //{
                            //    communication.SendMessage($"This nickname is in ban, write else {lastString}");
                            //    communication.AnswerClient();
                            //    continue;
                            //}
                            //return (communication.data.ToString(), true);
                            if (!await CheckNicknamesBan(communication.data.ToString()))
                            {
                                return communication.data.ToString();
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
        private bool CheckWantingToConnect()
        {
            communication.SendMessage("You really want to conect to the server, if yes - click Enter");
            communication.AnswerClient();
            if (communication.data.ToString() == "Enter")
            {
                return true;
            }
            return false;
        }
        private string CheckPassword()
        {
            while (true)
            {
                communication.AnswerClient();
                if (communication.data.Length > 7)
                {
                    communication.SendMessage("Ok");
                    return communication.data.ToString();
                }
                communication.SendMessage("Password length < 7, enter another password");
            }
        }
        //private async Task<bool> EnterPasswordAndSaveData(Socket listenSocket)
        //{
        //    StringBuilder nickname = new StringBuilder();
        //    nickname.Append(data);
        //    SendMessage("Enter password bigger than 7 symbols", listenSocket);
        //    AnswerClient(listenSocket);
        //    CheckDisconect();
        //    SendMessage("LastCheck", listenSocket);
        //    AnswerClient(listenSocket);
        //    bool successAdding = await AddNewUser(listenSocket, new string[] { nickname.ToString(), data.ToString() });
        //    return successAdding;
        //}
        private void CheckDisconect()
        {
            if (communication.data.ToString() == "?")
            {
                throw new Exception();
            }
        }
        private async Task<UserNicknameAndPasswordAndIPs> CheckNicknameAndPassword(IEnumerable<UserNicknameAndPasswordAndIPs> userData, bool needLastCheck)
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
                        if (CheckPassword(userNicknameAndPassword, needLastCheck))
                        {
                            return userNicknameAndPassword;
                        }
                        else
                        {
                            if (CheckWantingToConnect())
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
            return default(UserNicknameAndPasswordAndIPs);
        }
        private bool CheckPassword(UserNicknameAndPasswordAndIPs userNicknameAndPassword, bool needLastCheck)
        {
            var numberAttemps = 5;
            communication.SendMessage("Enter password bigger than 7 symbols");
            for (int i = 0; i < numberAttemps; i++)
            {
                communication.AnswerClient();
                if (communication.data.ToString() == userNicknameAndPassword.Password)
                {
                    communication.SendMessage("Ok");
                    communication.AnswerClient();
                    communication.SendMessage("LastCheck");
                    if (needLastCheck)
                    {
                        communication.AnswerClient();
                        communication.SendMessage("You enter to messenger");
                    };
                    nickname = userNicknameAndPassword.Nickname;
                    return true;
                }
                if (numberAttemps - i > 0)
                {
                    communication.SendMessage($"Wrong password, try again, number of attemps = {numberAttemps - i}");
                }
            }
            return false;
        }
        private async Task<bool> ConnectUsedNickname(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            var userNicknameAndPassword = await CheckNicknameAndPassword(userData, true);
            if (userNicknameAndPassword.Equals(default(UserNicknameAndPasswordAndIPs)))
            {
                return false;
            }
            await AddIP(userNicknameAndPassword);
            return true;
        }
        private async Task AddIP(UserNicknameAndPasswordAndIPs userNicknameAndPasswordAndIPs)
        {
            var haveIP = false;
            foreach (var IP in userNicknameAndPasswordAndIPs.IPs)
            {
                if (IP == this.IP)
                {
                    haveIP = true;
                }
            }
            if (!haveIP)
            {
                await fileMaster.ReadWrite(FilePath, users =>
                {
                    UserNicknameAndPasswordAndIPs userWithNewIP = new UserNicknameAndPasswordAndIPs();
                    foreach (var user in users)
                    {
                        if (user.Nickname == userNicknameAndPasswordAndIPs.Nickname)
                        {
                            userWithNewIP = user;
                            break;
                        }
                    }
                    users.Remove(userWithNewIP);
                    userWithNewIP.IPs.Add(IP);
                    users.Add(userWithNewIP);
                    return (users, true);
                });
                //List<UserNicknameAndPasswordAndIPs> users;
                //using (var stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Read))
                //{
                //    var usersJson = await AddFileToSB(stream);
                //    users = JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
                //}
                //UserNicknameAndPasswordAndIPs userWithNewIP = new UserNicknameAndPasswordAndIPs();
                //foreach (var user in users)
                //{
                //    if (user.Nickname == userNicknameAndPasswordAndIPs.Nickname)
                //    {
                //        userWithNewIP = user;
                //        break;
                //    }
                //}
                //users.Remove(userWithNewIP);
                //userWithNewIP.IPs.Add(IP);
                //users.Add(userWithNewIP);
                //using (var stream = new StreamWriter(FilePath, false))
                //{
                //    var usersJson = JsonConvert.SerializeObject(users);
                //    await stream.WriteAsync(usersJson);
                //}
            }
        }
        private void AnswerAndCheckDisconect()
        {
            communication.AnswerClient();
            CheckDisconect();
        }
        private bool FindNick(IEnumerable<UserNicknameAndPasswordAndIPs> userData)
        {
            var findNick = false;
            foreach (var userNicknameAndPassword in userData)
            {
                if (communication.data.ToString() == userNicknameAndPassword.Nickname)
                {
                    var numberAttemps = 5;
                    findNick = true;
                    communication.SendMessage("Enter password bigger than 7 symbols");
                    for (int i = 0; i < numberAttemps; i++)
                    {
                        communication.AnswerClient();
                        if (communication.data.ToString() == userNicknameAndPassword.Password)
                        {
                            communication.SendMessage("LastCheck");
                            AnswerAndCheckDisconect();
                            communication.SendMessage("You enter to messenger");
                            return true;
                        }
                        if (numberAttemps - i > 0)
                        {
                            communication.SendMessage($"Wrong password, try again, number of attemps = {numberAttemps - i}");
                        }
                        else
                        {
                            communication.SendMessage($"Number of attemps = {numberAttemps - i}," +
                                $"You really want to conect to the server, if yes - click Enter");
                        }
                    }
                }
            }
            if (!findNick)
            {
                communication.AnswerClient();
                communication.SendMessage("Wrong nickname, enter new");
            }
            return false;
        }
        private async Task<IEnumerable<UserNicknameAndPasswordAndIPs>> TakeAllUserData()
        {
            return await fileMaster.ReadAndDesToLUserInf(FilePath);
            //StringBuilder usersJson;
            //using (FileStream stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.Read))
            //{
            //    usersJson = await AddFileToSB(stream);
            //}
            //return JsonConvert.DeserializeObject<IEnumerable<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
        }
        //private async Task<StringBuilder> AddFileToSB(FileStream stream)
        //{
        //    StringBuilder usersJson = new StringBuilder();
        //    var buffer = 256;
        //    var position = stream.Seek(0, SeekOrigin.Begin);
        //    byte[] arrayBytes = new byte[buffer];
        //    while (true)
        //    {
        //        var readedRealBytes = await stream.ReadAsync(arrayBytes, 0, buffer);
        //        var stringArrayBytes = Encoding.Default.GetString(arrayBytes, 0, readedRealBytes);
        //        usersJson.Append(stringArrayBytes);
        //        if (readedRealBytes < buffer)
        //        {
        //            break;
        //        }
        //    }
        //    return usersJson;
        //}
        private async Task<bool> AddNewUser(IEnumerable<string> userData)
        {
            var IPs = new List<string>();
            IPs.Add(IP);
            //var IP = listener.RemoteEndPoint.ToString();                                          //////////////maybe something else
            UserNicknameAndPasswordAndIPs userNicknameAndPassword = new UserNicknameAndPasswordAndIPs(userData.First(), userData.Last(), IPs);
            var result = await fileMaster.ReadWrite(FilePath, oldData =>
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
            //using (FileStream stream = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            //{
            //    StringBuilder usersJson = await AddFileToSB(stream);
            //    var oldData = JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
            //    string userDataJson;
            //    if (oldData == null)
            //    {
            //        List<UserNicknameAndPasswordAndIPs> userNicknameAndPasswordList = new List<UserNicknameAndPasswordAndIPs>();
            //        userNicknameAndPasswordList.Add(userNicknameAndPassword);
            //        userDataJson = JsonConvert.SerializeObject(userNicknameAndPasswordList);
            //    }
            //    else
            //    {
            //        if (!LastCheck(oldData, userNicknameAndPassword.Nickname))
            //        {
            //            return false;
            //        }
            //        oldData.Add(userNicknameAndPassword);
            //        userDataJson = JsonConvert.SerializeObject(oldData);
            //    }
            //    byte[] arrayBytes = Encoding.Default.GetBytes(userDataJson);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    communication.SendMessage("You enter to messenger");
            //    stream.Write(arrayBytes, 0, arrayBytes.Length);
            //    fileMaster.CreateFolderAndFile($"{UserFoldersPath}\\{userNicknameAndPassword.Nickname}");
            //    nickname = userNicknameAndPassword.Nickname;
            //    return true;
            //}
        }
        private bool LastCheck(IEnumerable<UserNicknameAndPasswordAndIPs> usersData, string nick)
        {
            foreach (var userData in usersData)
            {
                if (nick == userData.Nickname)
                {
                    communication.SendMessage("This nickname is currently in use, enter new please\n\rEnter new nickname");
                    return false;
                }
            }
            return true;
        }
    }
}
