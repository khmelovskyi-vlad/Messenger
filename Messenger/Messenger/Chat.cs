using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    class Chat
    {
        public Chat(User user, string typtChat, string nameChat, string pathChat)
        {
            this.user = user;
            this.TypeChat = typtChat;
            this.NameChat = nameChat;
            this.PathChat = pathChat;
            message = "If you want exit, write: ?/end\n\r" +
                "If you want leave a group, write: ?/leave a group\n\r" +
                "If you want delete user, write: ?/delete user\n\r" +
                "If you want send file, write: ?/send\n\r" +
                "If you want download file, write: ?/download\n\r";
            if (TypeChat == "pp" || TypeChat == "ch")
            {
                message = $"{message}" +
                    $"If you want change chat to group, write: ?/change\n\r";
            }
        }
        public string NameChat { get; set; }

        public List<User> UsersOnline = new List<User>();
        private User user { get; }
        private string TypeChat { get; set; }
        private StringBuilder data;
        private byte[] buffer;
        const int size = 256;
        private object lockObj = new object();
        private List<string> messages;
        private string PathChat;
        private string message;

        public async Task Run(User user, bool firstConnect)
        {
            user.communication.SendMessage(message, user.Socket);
            user.communication.AnswerClient(user.Socket);
            if (firstConnect)
            {
                messages = await FirstRead();
            }
            FirstSentMessage(messages, user);
            UsersOnline.Add(user);
            while (true)
            {
                user.communication.AnswerClient(user.Socket);
                var message = user.communication.data.ToString();
                if (message == "?/end")
                {
                    UsersOnline.Remove(user);
                    return;
                }
                else if (message == "?/leave a group")
                {
                    GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, PathChat, TypeChat, user.communication, NameChat);
                    if (await groupsLeaver.Leave())
                    {
                        return;
                    }
                }
                else if (message == "?/delete user")
                {
                    await DeleteUser();
                }
                else if (message == "?/send")
                {
                    message = await ReciveFile();
                    if (message == "?")
                    {
                        continue;
                    }
                }
                else if (message == "?/download")
                {
                    SendFile();
                    continue;
                }
                else if (message == "?/change")
                {
                    if (TypeChat == "pp" || TypeChat == "ch")
                    {
                        await ChangeTypeGroup(user);
                        continue;
                    }
                }
                SendMessage(user, message);
            }
        }
        private void ChangeTypeGroup(string typeGroup)
        {
            if (typeGroup == "public")
            {
                TypeChat = "ug";
            }
            else if (typeGroup == "secret")
            {
                TypeChat = "sg";
            }
        }
        private async Task ChangeTypeGroup(User user)
        {
            UsersOnline.Remove(user);
            user.communication.SendMessage("Write type new group\n\r" +
                "If public - write public\n\r" +
                "If secret - write secret");
            user.communication.AnswerClient();
            var typeNewGroup = user.communication.data.ToString();
            string pathNewGroup;
            switch (typeNewGroup)
            {
                case "?":
                    UsersOnline.Add(user);
                    return;
                case "public":
                    pathNewGroup = @"D:\temp\messenger\publicGroup";
                    break;
                case "secret":
                    pathNewGroup = @"D:\temp\messenger\secretGroup";
                    break;
                default:
                    return;
            }
            user.communication.SendMessage("Write name new group");
            while (true)
            {
                user.communication.AnswerClient();
                var nameNewGroup = user.communication.data.ToString();
                if (nameNewGroup == "?")
                {
                    UsersOnline.Add(user);
                    return;
                }
                if (CheckGroups(nameNewGroup, pathNewGroup, user))
                {
                    var userGroup = await FindUserPath(user);
                    var userPath = $@"D:\temp\messenger\Users\{userGroup}";
                    var usersPaths = new string[] { $@"D:\temp\messenger\Users\{user.Nickname}", userPath };
                    await DeletePeopleChatsBeen(usersPaths);
                    await AddUserToGroups(usersPaths[0], nameNewGroup, typeNewGroup, user);
                    await AddInvitation(userPath, nameNewGroup, typeNewGroup, userGroup);
                    var newPath = $"{pathNewGroup}\\{nameNewGroup}";
                    Directory.Move(PathChat, newPath);
                    PathChat = newPath;
                    NameChat = nameNewGroup;
                    user.communication.SendMessage($"New group have {typeNewGroup} type and name {nameNewGroup}");
                    UsersOnline.Add(user);
                    return;
                }
            }
        }
        private async Task AddInvitation(string userPath, string nameGroup, string typeGroup, string userGroup)
        {
            using (var stream = File.Open($"{userPath}\\invitation.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var invitationsJsonSB = await ReadFile(stream);
                var invitations = JsonConvert.DeserializeObject<List<string>>(invitationsJsonSB.ToString());
                if (invitations == null)
                {
                    invitations = new List<string>();
                }
                invitations.Add($"{typeGroup}: {nameGroup}");
                stream.Seek(0, SeekOrigin.Begin);
                var invitationsJson = JsonConvert.SerializeObject(invitations);
                var buffer = Encoding.Default.GetBytes(invitationsJson);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            using (var stream = File.Open($"{PathChat}\\invitation.json", FileMode.Create, FileAccess.ReadWrite))
            {
                var invitationsJsonSB = await ReadFile(stream);
                var invitations = JsonConvert.DeserializeObject<List<string>>(invitationsJsonSB.ToString());
                if (invitations == null)
                {
                    invitations = new List<string>();
                }
                invitations.Add(userGroup);
                stream.Seek(0, SeekOrigin.Begin);
                var invitationsJson = JsonConvert.SerializeObject(invitations);
                var buffer = Encoding.Default.GetBytes(invitationsJson);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        private async Task AddUserToGroups(string userPath, string nameGroup, string typeGroup, User user)
        {
            string partPath;
            switch (typeGroup)
            {
                case "public":
                    partPath = "userGroups.json";
                    break;
                case "secret":
                    partPath = "secretGroups.json";
                    break;
                default:
                    return;
            }
            using (var stream = File.Open($"{userPath}\\{partPath}", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var groupsJsonSB = await ReadFile(stream);
                var groups = JsonConvert.DeserializeObject<List<string>>(groupsJsonSB.ToString());
                if (groups == null)
                {
                    groups = new List<string>();
                }
                groups.Add(nameGroup);
                stream.Seek(0, SeekOrigin.Begin);
                var groupsJson = JsonConvert.SerializeObject(groups);
                var buffer = Encoding.Default.GetBytes(groupsJson);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            List<string> users = new List<string>();
            users.Add(user.Nickname);
            var usersJson = JsonConvert.SerializeObject(users);
            using (var stream = new StreamWriter($"{PathChat}\\users.json", false))
            {
                await stream.WriteAsync(usersJson);
            }
        }
        private async Task<string> FindUserPath(User user)
        {
            List<string> usersGroup;
            using (var stream = File.Open($"{PathChat}\\users.json", FileMode.Open, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                usersGroup = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
            var usersPaths = new List<string>();
            foreach (var userGroup in usersGroup)
            {
                if (userGroup != user.Nickname)
                {
                    return userGroup;
                }
            }
            return "";
        }
        private async Task DeletePeopleChatsBeen(string[] usersPath)
        {
            foreach (var userPath in usersPath)
            {
                var path = $"{userPath}\\peopleChatsBeen.json";
                List<PersonChat> groups;
                using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    var groupsJsonSB = await ReadFile(stream);
                    groups = JsonConvert.DeserializeObject<List<PersonChat>>(groupsJsonSB.ToString());
                }
                PersonChat needGroup = new PersonChat(new string[2], "");
                foreach (var group in groups)
                {
                    if (group.NameChat == NameChat)
                    {
                        needGroup = group;
                        break;
                    }
                }
                groups.Remove(needGroup);
                var groupsJson = JsonConvert.SerializeObject(groups);
                using (var stream = new StreamWriter(path, false))
                {
                    await stream.WriteAsync(groupsJson);
                }
            }
        }
        private bool CheckGroups(string nameGroup, string path, User user)
        {
            foreach (var symbol in nameGroup)
            {
                if (symbol == '\\' || symbol == '/' || symbol == ':' || symbol == '*' || symbol == '?'
                        || symbol == '"' || symbol == '<' || symbol == '>' || symbol == '|')
                {
                    var invertedComma = '"';
                    user.communication.SendMessage($"Group name can not contain characters such as:\n\r" +
                        $"' ', '\\', '/', ':', '*', '?', '{invertedComma}', '<', '>', '|'\n\r" +
                        $"Enter new please");
                    return false;
                }
            }
            var groupsPath = Directory.GetDirectories(path);
            foreach (var groupPath in groupsPath)
            {
                var group = Path.GetFileName(groupPath);
                if (group == nameGroup)
                {
                    user.communication.SendMessage($"Have this group name, enter new please");
                    return false;
                }
            }
            return true;
        }
        private void SendFile()
        {
            UsersOnline.Remove(user);
            user.communication.SendMessage("ok");
            user.communication.AnswerClient();
            var fileName = user.communication.data.ToString();
            var filesPaths = Directory.GetFiles(PathChat);
            foreach (var filePath in filesPaths)
            {
                var file = Path.GetFileName(filePath);
                if (file.Length > 19)
                {
                    var fileWithoutData = file.Remove(0, 19);
                    if (fileWithoutData == fileName)
                    {
                        user.communication.SendMessage("Finded");
                        user.communication.AnswerClient();
                        user.Socket.SendFile(filePath);
                        UsersOnline.Add(user);
                        return;
                    }
                }
            }
            user.communication.SendMessage("Didn`t find");
            UsersOnline.Add(user);
        }
        private async Task<string> ReciveFile()
        {
            UsersOnline.Remove(user);
            user.communication.SendMessage("Check your file");
            user.communication.AnswerClient();
            var nameFile = user.communication.data.ToString();
            if (nameFile == "?")
            {
                UsersOnline.Add(user);
                return nameFile;
            }
            user.communication.SendMessage("Ok");
            var timeString = DateTime.Now.ToString();
            StringBuilder normalTime = new StringBuilder();
            foreach (var timeChar in timeString)
            {
                if (timeChar == ':')
                {
                    normalTime.Append('.');
                    continue;
                }
                normalTime.Append(timeChar);
            }
            var filePath = $@"{PathChat}\\{normalTime}{nameFile}";
            await WriteFile(filePath);
            UsersOnline.Add(user);
            return nameFile;
        }
        public async Task WriteFile(string path)
        {
            buffer = new byte[size];
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                do
                {
                    var size = user.Socket.Receive(buffer);
                    await stream.WriteAsync(buffer, 0, size);
                } while (user.Socket.Available > 0);
            }
        }
        private async Task DeleteUser()
        {
            while (true)
            {
                user.communication.SendMessage("Write user nickname");
                user.communication.AnswerClient(user.Socket);
                var nickname = user.communication.data.ToString();
                if (await CheckHavingNick(nickname))
                {
                    GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, PathChat, TypeChat, user.communication, NameChat);
                    if (await groupsLeaver.Leave())
                    {
                        user.communication.SendMessage("Deleted this user");
                        foreach (var userOnline in UsersOnline)
                        {
                            if (userOnline.Nickname == nickname)
                            {
                                userOnline.communication.SendMessage("?/delete user");
                            }
                        }
                        return;
                    }
                }
                else
                {
                    user.communication.SendMessage("Don`t have this nickname");
                }
            }
        }
        private async Task<bool> CheckHavingNick(string nickname)
        {
            List<string> users;
            using (var stream = File.Open($"{PathChat}\\users.json", FileMode.Open, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
            foreach (var user in users)
            {
                if (user == nickname)
                {
                    return true;
                }
            }
            return false;
        }
        private void FirstSentMessage(List<string> messages, User user)
        {
            user.communication.SendMessage(messages.Count.ToString(), user.Socket);
            user.communication.AnswerClient(user.Socket);
            foreach (var message in messages)
            {
                user.communication.SendMessage(message, user.Socket);
                user.communication.AnswerClient(user.Socket);
            }
        }
        private void CheckFile()
        {

        }
        private void SendMessage(User user, string message)
        {
            //var now = DateTime.Now.ToString();
            var messageSend = $"{user.Nickname}: {message}\n\r{DateTime.Now.ToString()}";
            SendMessageAllUsers(user, messageSend);
            lock (lockObj)
            {
                SaveMessage(messageSend);
            }
        }
        private void SendMessageAllUsers(User user, string message)
        {
            foreach (var userOnline in UsersOnline)
            {
                if (userOnline.Nickname != user.Nickname)
                {
                    userOnline.communication.SendMessage(message, userOnline.Socket);
                }
            }
        }
        private async void SaveMessage(string message)
        {
            using (var stream = File.Open($"{PathChat}\\data.json", FileMode.Open, FileAccess.Write))
            {
                messages.Add(message);
                var messagesJson = JsonConvert.SerializeObject(messages);
                var buffer = Encoding.Default.GetBytes(messagesJson);
                stream.Seek(0, SeekOrigin.Begin);
                await stream.WriteAsync(buffer, 0, buffer.Length);
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
        private async Task<List<string>> FirstRead()
        {
            using (var stream = File.Open($"{PathChat}\\data.json", FileMode.OpenOrCreate, FileAccess.Read))
            {
                var messagesJson = await ReadFile(stream);
                var messages = JsonConvert.DeserializeObject<List<string>>(messagesJson.ToString());
                if (messages == null)
                {
                    messages = new List<string>();
                }
                return messages;
            }
        }




        AutoResetEvent resetSend = new AutoResetEvent(false);
        AutoResetEvent resetReceive = new AutoResetEvent(false);
    }
}
