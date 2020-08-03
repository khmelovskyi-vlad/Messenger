using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Chat(string typtChat, string nameChat, string pathChat, Messenger messenger, FileMaster fileMaster)
        {
            this.TypeChat = typtChat;
            this.NameChat = nameChat;
            this.PathChat = pathChat;
            this.messenger = messenger;
            this.fileMaster = fileMaster;
        }
        public string NameChat { get; set; }
        private string TypeChat { get; set; }
        private string PathChat { get; set; }
        private Messenger messenger;
        private object usersOnlineLock = new object();
        public List<User> UsersOnline = new List<User>();
        private object usersOnlineToCheckLock = new object();
        public List<User> UsersOnlineToCheck = new List<User>();
        private List<string> messages;
        private object messagesLock = new object();
        private string message;
        private FileMaster fileMaster;

        public async Task Run(User user, bool firstConnect)
        {
            //Interlocked.Add()
            CreateMainMessage();
            await user.communication.AnswerClient();
            await user.communication.SendMessageAndAnswerClient(TypeChat);
            await user.communication.SendMessageAndAnswerClient(message);
            if (firstConnect)
            {
                await FirstRead();
            }
            await SendManyMessages(messages, user, messagesLock);
            lock (usersOnlineLock)
            {
                UsersOnline.Add(user);
            }
            try
            {
                await Communicate(user);
            }
            catch (Exception ex)
            {
                lock (usersOnlineLock)
                {
                    UsersOnline.Remove(user);
                }
                lock (usersOnlineToCheckLock)
                {
                    UsersOnlineToCheck.Remove(user);
                }
                throw ex;
            }
        }
        private async Task Communicate(User user)
        {
            while (true)
            {
                await user.communication.AnswerClient();
                var message = user.communication.data.ToString();
                switch (message)
                {
                    case "?/send":
                        await ReceiveFile(user);
                        continue;
                    case "?/download":
                        await SendFile(user);
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
                        lock (usersOnlineLock)
                        {
                            UsersOnline.Remove(user);
                        }
                        await user.communication.SendMessage("?/you left the chat");
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
            await user.communication.SendMessageAndAnswerClient("You really want to leave a group? If yes write: 'yes'");
            if (user.communication.data.ToString() == "yes")
            {
                GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, NameChat, PathChat, TypeChat, messenger.Server.UsersPath, fileMaster);
                await groupsLeaver.Leave();
                lock (usersOnlineToCheckLock)
                {
                    UsersOnlineToCheck.Remove(user);
                }
                await user.communication.SendMessage("?/you left the chat");
                return true;
            }
            else
            {
                await SendMessageAndAddUser("Ok, you didn't leave the chat", user);
                return false;
            }
        }
        private void RemoveUser(User user)
        {
            lock (usersOnlineLock)
            {
                UsersOnline.Remove(user);
            }
            lock (usersOnlineToCheckLock)
            {
                UsersOnlineToCheck.Add(user);
            }
        }
        private async Task AddUser(User user)
        {
            await user.communication.AnswerClient();
            await SendManyMessages(user.UnReadMessages, user, user.MessagesLock);
            lock (usersOnlineLock)
            {
                UsersOnline.Add(user);
            }
            lock (usersOnlineToCheckLock)
            {
                UsersOnlineToCheck.Remove(user);
            }
        }
        private void KickPeople(User user)
        {
            lock (usersOnlineLock)
            {
                foreach (var userOnline in UsersOnline)
                {
                    if (user.Nickname != userOnline.Nickname)
                    {
                        userOnline.communication.EndTask = true;
                    }
                }
                UsersOnline = new List<User>();
            }
            lock (usersOnlineToCheckLock)
            {
                foreach (var userOnlineToCheck in UsersOnlineToCheck)
                {
                    if (user.Nickname != userOnlineToCheck.Nickname)
                    {
                        userOnlineToCheck.communication.EndTask = true;
                    }
                }
                UsersOnlineToCheck = new List<User> { user };
            }
        }
        private void CreateMainMessage()
        {
            var firstPartOfMainMessage = $"If you want to exit, write: '?/end'{Environment.NewLine}" +
                    $"If you want to leave the group, write: '?/leave a group'{Environment.NewLine}" +
                    $"If you want to delete a user, write: '?/delete'{Environment.NewLine}" +
                    $"If you want to send a file, write: '?/send'{Environment.NewLine}" +
                    $"If you want to download a file, write: '?/download'{Environment.NewLine}";
            if (TypeChat == "pp" || TypeChat == "ch")
            {
                message = $"{firstPartOfMainMessage}" +
                    $"If you want to change the chat to the group, write: '?/change'{Environment.NewLine}";
            }
            else
            {
                message = $"{firstPartOfMainMessage}" +
                    $"If you want to invite somebody to the group, write: '?/invite'{Environment.NewLine}";
            }
        }
        private async Task InvitePerson(User user)
        {
            RemoveUser(user);
            await user.communication.SendMessageAndAnswerClient("Write the name of the person you want to add");
            var namePerson = user.communication.data.ToString();
            if (await CheckPerson(namePerson))
            {
                if (!await CheckUserPresenceGroup(namePerson, user))
                {
                    await AddInvites(namePerson);
                    await SendMessageAndAddUser("The person was invited", user);
                }
            }
            else
            {
                await SendMessageAndAddUser("Don`t have this person", user);
            }
        }
        private async Task SendMessageAndAddUser(string message, User user)
        {
            await user.communication.SendMessage($"{message},{Environment.NewLine}" +
                $"you are back in the chat");
            await AddUser(user);
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
            await fileMaster.UpdateFile(Path.Combine(messenger.Server.UsersPath, namePerson, "invitation.json"), fileMaster.AddData($"{partInvitation}{NameChat}"));
            await fileMaster.UpdateFile(Path.Combine(PathChat, namePerson, "invitation.json"), fileMaster.AddData(namePerson));
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
            return ((await fileMaster.ReadAndDeserialize<UserNicknameAndPasswordAndIPs>(Path.Combine(messenger.Server.NicknamesAndPasswordsPath, "users.json")))
                ?? new List<UserNicknameAndPasswordAndIPs>())
                .Select(user => user.Nickname)
                .Contains(namePerson);
        }
        private async Task<bool> CheckUserPresenceGroup(string namePerson, User user)
        {
            if (!await CheckUsersLeavedPeopleInvitation(Path.Combine(PathChat, "leavedPeople.json")))
            {
                if (!await CheckUsersLeavedPeopleInvitation(Path.Combine(PathChat, "users.json")))
                {
                    if (!await CheckUsersLeavedPeopleInvitation(Path.Combine(PathChat, "invitation.json")))
                    {
                        return false;
                    };
                    await SendMessageAndAddUser("This person has an invitation", user);
                    return true;
                };
                await SendMessageAndAddUser("This person is in the group", user);
                return true;
            };
            await SendMessageAndAddUser("This person leaved the group", user);
            return true;
            async Task<bool> CheckUsersLeavedPeopleInvitation(string path)
            {
                var users = await fileMaster.ReadAndDeserialize<string>(path);
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
            await user.communication.SendMessageAndAnswerClient($"Write the type of the new group{Environment.NewLine}" +
                $"If public - write 'public'{Environment.NewLine}" +
                "If secret - write 'secret'");
            var typeNewGroup = user.communication.data.ToString();
            string pathNewGroup;
            switch (typeNewGroup)
            {
                case "public":
                    pathNewGroup = messenger.Server.PublicGroupPath;
                    break;
                case "secret":
                    pathNewGroup = messenger.Server.SecretGroupPath;
                    break;
                default:
                    await AddUser(user);
                    return;
            }
            await user.communication.SendMessage("Write name new group");
            while (true)
            {
                await user.communication.AnswerClient();
                var nameNewGroup = user.communication.data.ToString();
                if (await CheckGroups(nameNewGroup, pathNewGroup, user))
                {
                    KickPeople(user);
                    var groupUser = await FindAnotherUser(user);
                    var userPath = Path.Combine(messenger.Server.UsersPath, groupUser);
                    var usersPaths = new string[] { Path.Combine(messenger.Server.UsersPath, user.Nickname), userPath };
                    await AddUserToGroups(usersPaths[0], nameNewGroup, typeNewGroup, user);
                    await DeletePeopleChatsBeen(usersPaths);
                    await AddInvitations(userPath, nameNewGroup, typeNewGroup, groupUser);
                    var newPath = Path.Combine(pathNewGroup, nameNewGroup);
                    Directory.Move(PathChat, newPath);
                    PathChat = newPath;
                    NameChat = nameNewGroup;
                    await user.communication.SendMessage($"New group have {typeNewGroup} type and name {nameNewGroup}");
                    ChangeTypeGroup(typeNewGroup);
                    CreateMainMessage();
                    await AddUser(user);
                    return;
                }
            }
        }
        private async Task AddInvitations(string userPath, string nameGroup, string typeGroup, string userGroup)
        {
            await AddInvitation(Path.Combine(userPath, "invitation.json"), $"{typeGroup}: {nameGroup}");
            await AddInvitation(Path.Combine(PathChat, "invitation.json"), userGroup);
        }
        private async Task AddInvitation(string path, string data)
        {
            await fileMaster.UpdateFile(path, fileMaster.AddData(data));
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
            await fileMaster.UpdateFile(Path.Combine(userPath, partPath), fileMaster.AddData(nameGroup));
            await fileMaster.UpdateFile<string>(Path.Combine(PathChat, "users.json"), users =>
            {
                return (new List<string>() { user.Nickname }, true);
            });
        }
        private async Task<string> FindAnotherUser(User user)
        {
            return (await fileMaster.ReadAndDeserialize<string>(Path.Combine(PathChat, "users.json")))
                .Where(x => x != user.Nickname)
                .FirstOrDefault();
        }
        private async Task DeletePeopleChatsBeen(string[] usersPath)
        {
            foreach (var userPath in usersPath)
            {
                await fileMaster.UpdateFile<PersonChat>(Path.Combine(userPath, "peopleChatsBeen.json"), groups =>
                {
                    return (groups.Where(group => group.NameChat != NameChat).ToList(), true);
                });
            }
        }
        private async Task<bool> CheckGroups(string nameGroup, string path, User user)
        {
            var goodInput = CharacterCheckers.CheckInput(nameGroup);
            if (goodInput)
            {
                var groupsPath = Directory.GetDirectories(path);
                foreach (var groupPath in groupsPath)
                {
                    var group = Path.GetFileName(groupPath);
                    if (group == nameGroup)
                    {
                        await user.communication.SendMessage($"Have this group name, enter new, please");
                        return false;
                    }
                }
                return true;
            }
            else
            {
                await user.communication.SendMessage($"The group name can only contain lowercase letters and numbers,{Environment.NewLine}" +
                    $"Enter new, please");
                return false;
            }
        }
        private async Task SendFile(User user)
        {
            RemoveUser(user);
            await user.communication.SendMessageAndAnswerClient("Write the file name");
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
                    findNeedFile = await FindNeedFile(needFilesPaths, user);
                }
                if (needFilesPaths.Count == 1 || findNeedFile)
                {
                    await user.communication.SendMessageAndAnswerClient("Finded");
                    await user.communication.SendFile2(needFilesPaths[0]);
                    await AddUser(user);
                    return;
                }
            }
            await SendMessageAndAddUser("Didn`t find", user);
        }
        private async Task<bool> FindNeedFile(List<string> filesPaths, User user)
        {
            var allData = new StringBuilder();
            var dates = new List<string>();
            foreach (var filePath in filesPaths)
            {
                var file = Path.GetFileName(filePath);
                var date = file.Substring(0, 19);
                dates.Add(date);
                allData.Append($"{date}{Environment.NewLine}");
            }
            await user.communication.SendMessageAndAnswerClient($"Have some files, chose date:{Environment.NewLine}" +
                $"{allData}Write need date");
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
            await user.communication.SendMessageAndAnswerClient("Check your file");
            var nameFile = user.communication.data.ToString();
            if (nameFile == "?/escape")
            {
                await AddUser(user);
                return;
            }
            await user.communication.SendMessage("Ok");
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
            var filePath = Path.Combine(PathChat, $"{normalTime}{nameFile}");
            var sw = new Stopwatch();
            sw.Start();
            await user.communication.ReceiveFile5(filePath);
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
            await SendMessageAndAddUser("The file has been sent", user);
            await SendMessageAllUsers(user.Nickname, nameFile);
        }
        private async Task DeleteUser(User user)
        {
            RemoveUser(user);
            await user.communication.SendMessageAndAnswerClient("Write user nickname");
            var nickname = user.communication.data.ToString();
            if (await CheckHavingNick(nickname))
            {
                if (nickname == user.Nickname)
                {
                    await SendMessageAndAddUser($"You can't delete yourself,{Environment.NewLine}" +
                        "If you want to delete yourself, write '?/leave a group'", user);
                    return;
                }
                KickPerson(nickname);
                GroupsLeaver groupsLeaver = new GroupsLeaver(nickname, NameChat, PathChat, TypeChat, messenger.Server.UsersPath, fileMaster);
                await groupsLeaver.Leave();
                await SendMessageAndAddUser("User was deleted", user);
            }
            else
            {
                await SendMessageAndAddUser("Don`t have this nickname", user);
            }
        }
        private void KickPerson(string nickname)
        {
            lock (usersOnlineLock)
            {
                foreach (var userOnline in UsersOnline)
                {
                    if (userOnline.Nickname == nickname)
                    {
                        userOnline.communication.EndTask = true;
                        break;
                    }
                }
                UsersOnline = UsersOnline.Where(user => user.Nickname != nickname).ToList();
            }
            lock (usersOnlineToCheckLock)
            {
                foreach (var userOnlineToCheck in UsersOnlineToCheck)
                {
                    if (userOnlineToCheck.Nickname == nickname)
                    {
                        userOnlineToCheck.communication.EndTask = true;
                        break;
                    }
                }
                UsersOnlineToCheck = UsersOnlineToCheck.Where(user => user.Nickname != nickname).ToList();
            }
        }
        private async Task<bool> CheckHavingNick(string nickname)
        {
            return ((await fileMaster.ReadAndDeserialize<string>(Path.Combine(PathChat, "users.json")))
                ?? new List<string>())
                .Contains(nickname);
        }
        private async Task SendManyMessages(List<string> messages, User user, object locker)
        {
            //lock (locker)
            //{
                await user.communication.SendMessageAndAnswerClient(messages.Count.ToString());
                foreach (var message in messages)
                {
                    await user.communication.SendMessageAndAnswerClient(message);
                }
                user.UnReadMessages = new List<string>();
            //}
        }
        private async Task SendMessageAllUsers(string userNickname, string message)
        {
            var messageSend = $"{userNickname}: {message}{Environment.NewLine}" +
                $"{DateTime.Now.ToString()}";
            //lock (usersOnlineLock)
            //{
                foreach (var userOnline in UsersOnline)
                {
                    await userOnline.communication.SendMessage(messageSend);
                }
            //}
            lock (usersOnlineToCheckLock)
            {
                foreach (var userOnlineToCheck in UsersOnlineToCheck)
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
            await fileMaster.UpdateFile(Path.Combine(PathChat, "data.json"), fileMaster.AddData(message));
        }
        private async Task FirstRead()
        {
            this.messages = await fileMaster.ReadAndDeserialize<string>(Path.Combine(PathChat, "data.json")) ?? new List<string>();
        }




        AutoResetEvent resetSend = new AutoResetEvent(false);
        AutoResetEvent resetReceive = new AutoResetEvent(false);
    }
}
