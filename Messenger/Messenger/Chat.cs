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
        public Chat(string typtChat, string nameChat, string pathChat)
        {
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
        public List<User> UsersOnlineToCheck = new List<User>();
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
            //Interlocked.Add()
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
                    await DeleteUser(user);
                }
                else if (message == "?/send")
                {
                    message = await ReciveFile(user);
                    if (message == "?")
                    {
                        continue;
                    }
                }
                else if (message == "?/download")
                {
                    SendFile(user);
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
                else if (message == "?/invite")
                {
                    if (TypeChat == "pg" || TypeChat == "ug" || TypeChat == "sg")
                    {
                        await InvitePerson(user);
                        continue;
                    }
                }
                SendMessage(user, message);
            }
        }
        private void RemoveUser(User user)
        {
            UsersOnlineToCheck.Add(user);
            UsersOnline.Remove(user);
        }
        private void AddUser(User user)
        {
            UsersOnline.Add(user);
            UsersOnlineToCheck.Remove(user);
        }
        private async Task InvitePerson(User user)
        {
            RemoveUser(user);
            user.communication.SendMessage("Write the name of the person you want to add");
            user.communication.AnswerClient();
            var namePerson = user.communication.data.ToString();
            if (namePerson == "?")
            {
                AddUser(user);
                return;
            }
            if (await CheckPerson(namePerson))
            {
                if (await CheckUsersOrleavedPeopleOrInvitation(namePerson, $@"{PathChat}\leavedPeople.json"))
                {
                    if (await CheckUsersOrleavedPeopleOrInvitation(namePerson, $@"{PathChat}\users.json"))
                    {
                        if (await CheckUsersOrleavedPeopleOrInvitation(namePerson, $@"{PathChat}\invitation.json"))
                        {
                            await AddInvites(namePerson);
                            AddUser(user);
                            user.communication.SendMessage("Invited person");
                            return;
                        }
                        user.communication.SendMessage("This person has invitation");
                        return;
                    }
                    user.communication.SendMessage("This person is in group");
                    return;
                }
                user.communication.SendMessage("This person leaved the group");
                return;
            }
            user.communication.SendMessage("Don`t have this person");
        }
        private async Task AddInvites(string namePerson)
        {
            List<string> userInvitations;
            var userPath = $@"D:\temp\messenger\Users\{namePerson}\invitation.json";
            using (var stream = File.Open(userPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var invitationsJsonSb = await ReadFile(stream);
                userInvitations = JsonConvert.DeserializeObject<List<string>>(invitationsJsonSb.ToString());
            }
            string partInvitation;
            switch (TypeChat)
            {
                case "pg":
                    partInvitation = "public: ";
                    break;
                case "ug":
                    partInvitation = "public: ";
                    break;
                case "sg":
                    partInvitation = "secret: ";
                    break;
                default:
                    partInvitation = "";
                    break;
            }
            if (userInvitations == null)
            {
                userInvitations = new List<string>();
            }
            userInvitations.Add($"{partInvitation}{NameChat}");
            await WriteData(userPath, JsonConvert.SerializeObject(userInvitations));
            List<string> groupInvitations;
            var groupPath = $@"{PathChat}\invitation.json";
            using (var stream = File.Open(groupPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var invitationsJsonSb = await ReadFile(stream);
                groupInvitations = JsonConvert.DeserializeObject<List<string>>(invitationsJsonSb.ToString());
            }
            if (groupInvitations == null)
            {
                groupInvitations = new List<string>();
            }
            groupInvitations.Add(namePerson);
            await WriteData(groupPath, JsonConvert.SerializeObject(groupInvitations));
        }
        private async Task WriteData(string path, string data)
        {
            using (var stream = new StreamWriter(path, false))
            {
                await stream.WriteAsync(data);
            }
        }
        private async Task<bool> CheckPerson(string namePerson)
        {
            List<UserNicknameAndPasswordAndIPs> users;
            using (var stream = File.Open(@"D:\temp\messenger\nicknamesAndPasswords\users.json", FileMode.OpenOrCreate, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
            }
            foreach (var user in users)
            {
                if (user.Nickname == namePerson)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> CheckUsersOrleavedPeopleOrInvitation(string namePerson, string path)
        {
            List<string> users;
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
            if (users == null)
            {
                return true;
            }
            foreach (var user in users)
            {
                if (user == namePerson)
                {
                    return false;
                }
            }
            return true;
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
            RemoveUser(user);
            user.communication.SendMessage("Write type new group\n\r" +
                "If public - write public\n\r" +
                "If secret - write secret");
            user.communication.AnswerClient();
            var typeNewGroup = user.communication.data.ToString();
            string pathNewGroup;
            switch (typeNewGroup)
            {
                case "?":
                    AddUser(user);
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
                    AddUser(user);
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
                    AddUser(user);
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
        private void SendFile(User user)
        {
            RemoveUser(user);
            user.communication.SendMessage("Write chat name");
            user.communication.AnswerClient();
            var fileName = user.communication.data.ToString();
            var filesPaths = Directory.GetFiles(PathChat);
            var needFilesPaths = new List<string>();
            foreach (var filePath in filesPaths)
            {
                var file = Path.GetFileName(filePath);
                if (file.Length > 19)
                {
                    var fileWithoutDate = file.Remove(0, 19);
                    if (fileWithoutDate == fileName)
                    {
                        needFilesPaths.Add(filePath);
                    }
                }
            }
            if (needFilesPaths.Count > 0)
            {
                var findNeedFile = false;
                if (needFilesPaths.Count > 1)
                {
                    findNeedFile = FindNeedFile(needFilesPaths, user);
                }
                if (needFilesPaths.Count == 1 || findNeedFile)
                {
                    user.communication.SendMessage("Finded");
                    user.communication.AnswerClient();
                    user.Socket.SendFile(needFilesPaths[0]);
                    AddUser(user);
                    return;
                }
            }
            user.communication.SendMessage("Didn`t find");
            AddUser(user);
        }
        private bool FindNeedFile(List<string> filesPaths, User user)
        {
            var allData = new StringBuilder();
            var dates = new List<string>();
            foreach (var filePath in filesPaths)
            {
                var file = Path.GetFileName(filePath);
                var date = file.Substring(0, 19);
                dates.Add(date);
                allData.Append($"{date}\n\r");
            }
            user.communication.SendMessage($"Have some files, chose date:\n\r{allData}Write need date");
            user.communication.AnswerClient();
            var message = user.communication.data.ToString();
            foreach (var date in dates)
            {
                if (date == message)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<string> ReciveFile(User user)
        {
            RemoveUser(user);
            user.communication.SendMessage("Check your file");
            user.communication.AnswerClient();
            var nameFile = user.communication.data.ToString();
            if (nameFile == "?")
            {
                AddUser(user);
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
            await WriteFile(filePath, user);
            AddUser(user);
            return nameFile;
        }
        public async Task WriteFile(string path, User user)
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
        private async Task DeleteUser(User user)
        {
            while (true)
            {
                user.communication.SendMessage("Write user nickname");
                user.communication.AnswerClient(user.Socket);
                var nickname = user.communication.data.ToString();
                if (await CheckHavingNick(nickname))
                {
                    GroupsLeaver groupsLeaver = new GroupsLeaver(nickname, PathChat, TypeChat, user.communication, NameChat);
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
                //if (userOnline.Nickname != user.Nickname)
                //{
                    userOnline.communication.SendMessage(message, userOnline.Socket);
                //}
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
