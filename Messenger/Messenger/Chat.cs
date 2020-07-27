using Newtonsoft.Json;
using System;
using System.Collections;
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
        }
        public string NameChat { get; set; }
        public List<User> UsersOnline = new List<User>();
        public List<User> UsersOnlineToCheck = new List<User>();
        private string TypeChat { get; set; }
        private StringBuilder data;
        private byte[] buffer;
        const int size = 256;
        private List<string> messages;
        private object messagesLock = new object();
        private string PathChat;
        private string message;
        private FileMaster fileMaster = new FileMaster();

        public async Task Run(User user, bool firstConnect)
        {
            //Interlocked.Add()
            CreateMainMessage();
            user.communication.SendMessageAndAnswerClient(TypeChat);
            user.communication.SendMessageAndAnswerClient(message);
            if (firstConnect)
            {
                await FirstRead();
            }
            SendManyMessages(messages, user, messagesLock);
            UsersOnline.Add(user);
            while (true)
            {
                user.communication.AnswerClient();
                var message = user.communication.data.ToString();
                switch (message)
                {
                    case "?/send":
                        await ReceiveFile(user);
                        continue;
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
                    case "?/delete":
                        await DeleteUser(user);
                        continue;
                    case "?/leave a group":
                        if (await LeaveGroup(user))
                        {
                            return;
                        }
                        continue;
                    case "?/end":
                        UsersOnline.Remove(user);
                        user.communication.SendMessage("?/you left the chat");
                        return;
                    default:
                        break;
                }
                await SendMessageAllUsers(user.Nickname, message);
            }
        }
        private async Task<bool> LeaveGroup(User user)
        {
            RemoveUser(user);
            user.communication.SendMessageAndAnswerClient("You really want to leave a group? If yes write: 'yes'");
            if (user.communication.data.ToString() == "yes")
            {
                GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, PathChat, TypeChat, NameChat, fileMaster);
                await groupsLeaver.Leave();
                UsersOnlineToCheck.Remove(user);
                user.communication.SendMessage("?/you left the chat");
                return true;
            }
            else
            {
                SendMessageAndAddUser("Ok, you didn't leave the chat", user);
                return false;
            }
        }
        private void RemoveUser(User user)
        {
            UsersOnline.Remove(user);
            UsersOnlineToCheck.Add(user);
        }
        private void AddUser(User user)
        {
            user.communication.AnswerClient();
            SendManyMessages(user.UnReadMessages, user, user.MessagesLock);
            UsersOnline.Add(user);
            UsersOnlineToCheck.Remove(user);
        }
        private void CreateMainMessage()
        {
            var firstPartOfMainMessage = "If you want to exit, write: '?/end'\n\r" +
                    "If you want to leave the group, write: '?/leave a group'\n\r" +
                    "If you want to delete a user, write: '?/delete'\n\r" +
                    "If you want to send a file, write: '?/send'\n\r" +
                    "If you want to download a file, write: '?/download'\n\r";
            if (TypeChat == "pp" || TypeChat == "ch")
            {
                message = $"{firstPartOfMainMessage}" +
                    $"If you want to change the chat to the group, write: '?/change'\n\r";
            }
            else
            {
                message = $"{firstPartOfMainMessage}" +
                    $"If you want to invite somebody to the group, write: '?/invite'\n\r";
            }
        }
        private async Task InvitePerson(User user)
        {
            RemoveUser(user);
            user.communication.SendMessageAndAnswerClient("Write the name of the person you want to add");
            var namePerson = user.communication.data.ToString();
            if (await CheckPerson(namePerson))
            {
                if (!await CheckUserPresenceGroup(namePerson, user))
                {
                    await AddInvites(namePerson);
                    SendMessageAndAddUser("The person was invited", user);
                }
            }
            else
            {
                SendMessageAndAddUser("Don`t have this person", user);
            }
        }
        private void SendMessageAndAddUser(string message, User user)
        {
            user.communication.SendMessage($"{message},\n\r" +
                $"you are back in the chat");
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
            return ((await fileMaster.ReadAndDesToLUserInf(@"D:\temp\messenger\nicknamesAndPasswords\users.json"))
                ?? new List<UserNicknameAndPasswordAndIPs>())
                .Select(user => user.Nickname)
                .Contains(namePerson);
        }
        private async Task<bool> CheckUserPresenceGroup(string namePerson, User user)
        {
            if (!await CheckUsersLeavedPeopleInvitation($@"{PathChat}\leavedPeople.json"))
            {
                if (!await CheckUsersLeavedPeopleInvitation($@"{PathChat}\users.json"))
                {
                    if (!await CheckUsersLeavedPeopleInvitation($@"{PathChat}\invitation.json"))
                    {
                        return false;
                    };
                    SendMessageAndAddUser("This person has an invitation", user);
                    return true;
                };
                SendMessageAndAddUser("This person is in the group", user);
                return true;
            };
            SendMessageAndAddUser("This person leaved the group", user);
            return true;
            async Task<bool> CheckUsersLeavedPeopleInvitation(string path)
            {
                var users = await fileMaster.ReadAndDesToLString(path);
                if (users == null)
                {
                    users = new List<string>();
                }
                var result = users.Contains(namePerson);
                return result;
                //return ((await fileMaster.ReadAndDesToLString(path))
                //    ?? new List<string>())
                //    .Contains(namePerson);
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
            RemoveUser(user);
            user.communication.SendMessageAndAnswerClient("Write the type of the new group\n\r" +
                "If public - write 'public'\n\r" +
                "If secret - write 'secret'");
            var typeNewGroup = user.communication.data.ToString();
            string pathNewGroup;
            switch (typeNewGroup)
            {
                case "public":
                    pathNewGroup = @"D:\temp\messenger\publicGroup";
                    break;
                case "secret":
                    pathNewGroup = @"D:\temp\messenger\secretGroup";
                    break;
                default:
                    AddUser(user);
                    return;
            }
            user.communication.SendMessage("Write name new group");
            while (true)
            {
                user.communication.AnswerClient();
                var nameNewGroup = user.communication.data.ToString();
                if (CheckGroups(nameNewGroup, pathNewGroup, user))
                {
                    var groupUser = await FindAnotherUser(user);
                    var userPath = $@"D:\temp\messenger\Users\{groupUser}";
                    var usersPaths = new string[] { $@"D:\temp\messenger\Users\{user.Nickname}", userPath };
                    await AddUserToGroups(usersPaths[0], nameNewGroup, typeNewGroup, user);
                    await DeletePeopleChatsBeen(usersPaths);
                    await AddInvitations(userPath, nameNewGroup, typeNewGroup, groupUser);
                    var newPath = $"{pathNewGroup}\\{nameNewGroup}";
                    Directory.Move(PathChat, newPath);
                    PathChat = newPath;
                    NameChat = nameNewGroup;
                    user.communication.SendMessage($"New group have {typeNewGroup} type and name {nameNewGroup}");
                    ChangeTypeGroup(typeNewGroup);
                    CreateMainMessage();
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
            await fileMaster.ReadWrite($"{PathChat}\\users.json", users =>
            {
                return (new List<string>() { user.Nickname }, true);
            });
        }
        private async Task<string> FindAnotherUser(User user)
        {
            return (await fileMaster.ReadAndDesToLString($"{PathChat}\\users.json"))
                .Where(x => x != user.Nickname)
                .FirstOrDefault();
        }
        private async Task DeletePeopleChatsBeen(string[] usersPath)
        {
            foreach (var userPath in usersPath)
            {
                await fileMaster.ReadWrite($"{userPath}\\peopleChatsBeen.json", groups =>
                {
                    return (groups.Where(group => group.NameChat != NameChat).ToList(), true);
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
                        $"Enter new, please");
                    return false;
                }
            }
            var groupsPath = Directory.GetDirectories(path);
            foreach (var groupPath in groupsPath)
            {
                var group = Path.GetFileName(groupPath);
                if (group == nameGroup)
                {
                    user.communication.SendMessage($"Have this group name, enter new, please");
                    return false;
                }
            }
            return true;
        }
        private void SendFile(User user)
        {
            RemoveUser(user);
            user.communication.SendMessageAndAnswerClient("Write the file name");
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
                    user.communication.SendMessageAndAnswerClient("Finded");
                    user.communication.SendFile(needFilesPaths[0]);
                    AddUser(user);
                    return;
                }
            }
            SendMessageAndAddUser("Didn`t find", user);
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
            user.communication.SendMessageAndAnswerClient($"Have some files, chose date:\n\r{allData}Write need date");
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
        private async Task ReceiveFile(User user)
        {
            RemoveUser(user);
            user.communication.SendMessageAndAnswerClient("Check your file");
            var nameFile = user.communication.data.ToString();
            if (nameFile == "?/escape")
            {
                AddUser(user);
                return;
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
            user.communication.ReceiveFile3(filePath);
            SendMessageAndAddUser("The file has been sent", user);
            await SendMessageAllUsers(user.Nickname, nameFile);
        }
        private async Task DeleteUser(User user)
        {
            RemoveUser(user);
            user.communication.SendMessageAndAnswerClient("Write user nickname");
            var nickname = user.communication.data.ToString();
            if (await CheckHavingNick(nickname))
            {
                if (nickname == user.Nickname)
                {
                    SendMessageAndAddUser("You can't delete yourself,\n\r" +
                        "If you want to delete yourself, write '?/leave a group'", user);
                    return;
                }
                foreach (var userOnline in UsersOnline)
                {
                    if (userOnline.Nickname == nickname)
                    {
                        userOnline.communication.SendMessage("?/delete");
                        break;
                    }
                }
                GroupsLeaver groupsLeaver = new GroupsLeaver(nickname, PathChat, TypeChat, NameChat, fileMaster);
                await groupsLeaver.Leave();
                SendMessageAndAddUser("User was deleted", user);
            }
            else
            {
                SendMessageAndAddUser("Don`t have this nickname", user);
            }
        }
        private async Task<bool> CheckHavingNick(string nickname)
        {
            return ((await fileMaster.ReadAndDesToLString($"{PathChat}\\users.json"))
                ?? new List<string>())
                .Contains(nickname);
        }
        private void SendManyMessages(List<string> messages, User user, object locker)
        {
            lock (locker)
            {
                user.communication.SendMessageAndAnswerClient(messages.Count.ToString());
                foreach (var message in messages)
                {
                    user.communication.SendMessageAndAnswerClient(message);
                }
            }
        }
        private async Task SendMessageAllUsers(string userNickname, string message)
        {
            var messageSend = $"{userNickname}: {message}\n\r{DateTime.Now.ToString()}";
            foreach (var userOnline in UsersOnline)
            {
                userOnline.communication.SendMessage(messageSend, userOnline.Socket);
            }
            foreach (var userOnlineToCheck in UsersOnlineToCheck)
            {
                lock (userOnlineToCheck.MessagesLock)
                {
                    userOnlineToCheck.UnReadMessages.Add(messageSend);
                }
            }
            await SaveMessage(messageSend);
            lock (messagesLock)
            {
                messages.Add(messageSend);
            }
        }
        private async Task SaveMessage(string message)
        {
            await fileMaster.ReadWrite($"{PathChat}\\data.json", (fileMessages) =>
            {
                if (fileMessages == null)
                {
                    fileMessages = new List<string>();
                }
                fileMessages.Add(message);
                return (fileMessages, true);
            });
        }
        private async Task FirstRead()
        {
            this.messages = await fileMaster.ReadAndDesToLString($"{PathChat}\\data.json") ?? new List<string>();
        }




        AutoResetEvent resetSend = new AutoResetEvent(false);
        AutoResetEvent resetReceive = new AutoResetEvent(false);
    }
}
