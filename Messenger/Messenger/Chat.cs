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
            else
            {
                message = $"{message}" +
                    $"If you want invite somebody to group, write: ?/invite\n\r";
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
        FileMaster fileMaster = new FileMaster();

        public async Task Run(User user, bool firstConnect)
        {
            //Interlocked.Add()
            user.communication.SendMessage(TypeChat, user.Socket);
            user.communication.AnswerClient(user.Socket);
            user.communication.SendMessage(message, user.Socket);
            user.communication.AnswerClient(user.Socket);
            if (firstConnect)
            {
                await FirstRead();
            }
            FirstSentMessage(messages, user);
            UsersOnline.Add(user);
            while (true)
            {
                user.communication.AnswerClient(user.Socket);
                var message = user.communication.data.ToString();
                switch (message)
                {
                    case "?/end":
                        UsersOnline.Remove(user);
                        return;
                    case "?/leave a group":
                        GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, PathChat, TypeChat, NameChat);
                        if (await groupsLeaver.Leave())
                        {
                            return;
                        }
                        break;
                    case "?/delete user":
                        await DeleteUser(user);
                        continue;
                    case "?/send":
                        message = await ReciveFile(user);
                        if (message == "?")
                        {
                            continue;
                        }
                        break;
                    case "?/download":
                        SendFile(user);
                        continue;
                    case "?/change":
                        if (TypeChat == "pp" || TypeChat == "ch")
                        {
                            await ChangeTypeGroup(user);
                            continue;
                        }
                        break;
                    case "?/invite":
                        if (TypeChat == "pg" || TypeChat == "ug" || TypeChat == "sg")
                        {
                            await InvitePerson(user);
                            continue;
                        }
                        break;
                    default:
                        break;
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
                            SendMessageAndAddUser("Invited person", user);
                            return;
                        }
                        SendMessageAndAddUser("This person has invitation", user);
                        return;
                    }
                    SendMessageAndAddUser("This person is in group", user);
                    return;
                }
                SendMessageAndAddUser("This person leaved the group", user);
                return;
            }
            SendMessageAndAddUser("Don`t have this person", user);
        }
        private void SendMessageAndAddUser(string message, User user)
        {
            user.communication.SendMessage(message);
            AddUser(user);
        }
        private async Task AddInvites(string namePerson)
        {
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
            await fileMaster.ReadWrite($@"D:\temp\messenger\Users\{namePerson}\invitation.json", userInvitations =>
            {
                if (userInvitations == null)
                {
                    userInvitations = new List<string>();
                }
                userInvitations.Add($"{partInvitation}{NameChat}");
                return (userInvitations, true);
            });
            await fileMaster.ReadWrite($@"{PathChat}\invitation.json", groupInvitations =>
            {
                if (groupInvitations == null)
                {
                    groupInvitations = new List<string>();
                }
                groupInvitations.Add(namePerson);
                return (groupInvitations, true);
            });
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
            var users = await fileMaster.ReadAndDesToLUserInf(@"D:\temp\messenger\nicknamesAndPasswords\users.json");
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
            var users = await fileMaster.ReadAndDesToLString(path);
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
                    await AddInvitations(userPath, nameNewGroup, typeNewGroup, userGroup);
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
        private async Task AddInvitations(string userPath, string nameGroup, string typeGroup, string userGroup)
        {
            await AddInvitation($"{userPath}\\invitation.json", $"{typeGroup}: {nameGroup}");
            await AddInvitation($"{PathChat}\\invitation.json", userGroup);
        }
        private async Task AddInvitation(string path, string data)
        {
            await fileMaster.ReadWrite(path, invitations =>
            {
                if (invitations == null)
                {
                    invitations = new List<string>();
                }
                invitations.Add(data);
                return (invitations, true);
            });
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
            await fileMaster.ReadWrite($"{userPath}\\{partPath}", groups =>
            {
                if (groups == null)
                {
                    groups = new List<string>();
                }
                groups.Add(nameGroup);
                return (groups, true);
            });
            //await fileMaster.ReadWrite($"{PathChat}\\users.json", users =>
            //{
            //    users = new List<string>() { user.Nickname };
            //    return (users, true);
            //});
            List<string> users = new List<string>() { user.Nickname };
            var usersJson = JsonConvert.SerializeObject(users);
            using (var stream = new StreamWriter($"{PathChat}\\users.json", false))
            {
                await stream.WriteAsync(usersJson);
            }
        }
        private async Task<string> FindUserPath(User user)
        {
            var usersGroup = await fileMaster.ReadAndDesToLString($"{PathChat}\\users.json");
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
                await fileMaster.ReadWrite($"{userPath}\\peopleChatsBeen.json", groups =>
                {
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
                    return (groups, true);
                });
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
                    GroupsLeaver groupsLeaver = new GroupsLeaver(nickname, PathChat, TypeChat, NameChat);
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
            var users = await fileMaster.ReadAndDesToLString($"{PathChat}\\users.json");
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
        private async Task FirstRead()
        {
            var messages = await fileMaster.ReadAndDesToLString($"{PathChat}\\data.json");
            if (messages == null)
            {
                messages = new List<string>();
            }
            this.messages = messages;
        }




        AutoResetEvent resetSend = new AutoResetEvent(false);
        AutoResetEvent resetReceive = new AutoResetEvent(false);
    }
}
