using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class GroupMaster
    {
        public GroupMaster(Socket listener, string nickname)
        {
            this.listener = listener;
            this.nickname = nickname;
            communication = new Communication(listener);
        }
        private Socket listener;
        private string nickname;
        Communication communication;
        private string UserFoldersPath { get { return @"D:\temp\messenger\Users"; } }
        private string PublicGroupsPath { get { return @"D:\temp\messenger\publicGroup"; } }
        private string SecreatGroupsPath { get { return @"D:\temp\messenger\secretGroup"; } }
        private string PeopleChatsPath { get { return @"D:\temp\messenger\peopleChats"; } }
        public async Task<string[]> Run()
        {
            var enteringChats = await FindAllChats(nickname);
            SendChats(listener, enteringChats, false);
            //communication.SendMessage("If you want to leave the group write 'leave'\n\r" +
            //    "If you want to choose the group write 'choose'", listener);
            //communication.AnswerClient(listener);
            //while (true)
            //{
            //    if (communication.data.ToString() == "Select the group you want to leave")
            //    {
            //        communication.SendMessage("Choose group", listener);
            //        communication.AnswerClient(listener);
            //        GroupsLeaver groupsLeaver = new GroupsLeaver();

            //    }
            //    else if(communication.data.ToString() == "choose")
            //    {
            //        communication.SendMessage("Choose group", listener);
            //        communication.AnswerClient(listener);
                    return await ModeSelection(listener, enteringChats, nickname);
            //    }
            //}
        }
        private async Task<string[]> ModeSelection(Socket listener, List<string>[] allChats, string nick)
        {
            while (true)
            {
                communication.SendMessage("Enter type of chat, what do you need\n\r" +
                    "pp - people chat, ch - created chat with people, sg - secret group, ug - user group, pg - public group\n\r" +
                    "ii - accept the invitation\n\r" +
                    "Helper:\n\r" +
                    "Write all chats: ?/cc, find chat: ?/cc...\n\r" +
                    "Write all people: ?/pp, find people: ?/pp...\n\r" +
                    "Write all chat with people: ?/ch, find chat with people: ?/ch...\n\r" +
                    "Write all groups: ?/gg, find group: ?/gg...\n\r" +
                    "Write all secreat groups: ?/sg, find secreat group: ?/sg...\n\r" +
                    "Write all user groups: ?/ug, find user group: ?/ug...\n\r" +
                    "Write all public groups: ?/pg, find public group: ?/pg...\n\r" +
                    "Write all invitations: ?/ii, find invitation: ?/ii...\n\r" +
                    "If you want create new group, write ?/ng\n\r" +
                    "If you want exit, write: exit\n\r", listener);
                communication.AnswerClient(listener);
                var message = communication.data.ToString();
                if (message.Length > 3 && message[0] == '?' && message[1] == '/')
                {
                    communication.SendMessage("Ok");
                    communication.AnswerClient();
                    if (message.Length == 4)
                    {
                        var information = await ChoseGroupSend(listener, allChats, message, nick);
                        if (information.Length == 3)
                        {
                            return information;
                        }
                        continue;
                    }
                    else
                    {
                        HelpFindChat(listener, allChats, message);
                    }
                }
                else
                {
                    if (message == "exit")
                    {
                        communication.SendMessage("You exit messanger");
                        return new string[0];
                    }
                    if (message == "ii")
                    {
                        return await AcceptTheInvitation();
                    }
                    else if (message == "pp" || message == "ch" || message == "sg" || message == "ug" || message == "pg")
                    {
                        return await FindNeedGroup(message, message);
                    }
                }
            }
            return new string[0];
        }
        private async Task<string[]> FindNeedGroup(string message, string typeGroup)
        {
            string nameGroup;
            string pathGroup;
            string[] pathAndNAme;
            communication.SendMessage("Enter name of chat", listener);
            while (true)
            {
                communication.AnswerClient(listener);
                pathAndNAme = await CheckChatAndCreatePath(communication.data.ToString(), typeGroup);
                if (pathAndNAme[0] != "")
                {
                    communication.SendMessage("You connect to chat", listener);
                    pathGroup = pathAndNAme[0];
                    nameGroup = pathAndNAme[1];
                    break;
                }
                communication.SendMessage("Don`t have this chat, enter new please", listener);
            }
            return new string[] { typeGroup, nameGroup, pathGroup };
        }
        private string[] FindPathChat(string namePerson, List<PersonChat> peopleChatsBeenJson)
        {
            foreach (var peopleChatBeenJson in peopleChatsBeenJson)
            {
                if (peopleChatBeenJson.Nicknames[0] == namePerson || peopleChatBeenJson.Nicknames[1] == namePerson)
                {
                    return new string[] { $@"D:\temp\messenger\peopleChats\{peopleChatBeenJson.NameChat}", peopleChatBeenJson.NameChat};
                }
            }
            return new string[] { "", "" };
        }
        private async Task<string[]> CreateChat(string namePerson, List<string> people)
        {
            //var pathAndName = await CheckPeopleChats(namePerson);
            //if (pathAndName[0] != "")
            //{
            //    return pathAndName;
            //}
            var nameChat = $"{namePerson} {nickname}";
            PersonChat personChat = new PersonChat(new string[] { namePerson, nickname }, nameChat);
            WriteNewPerson($@"D:\temp\messenger\Users\{nickname}\peopleChatsBeen.json", personChat);
            WriteNewPerson($@"D:\temp\messenger\Users\{namePerson}\peopleChatsBeen.json", personChat);
            var path = $@"D:\temp\messenger\peopleChats\{nameChat}";
            Directory.CreateDirectory(path);
            await AddUser($"{path}\\users.json", nickname);
            await AddUser($"{path}\\users.json", namePerson);
            return new string[] { path, nameChat };
        }
        //private async Task<string[]> CheckPeopleChats(string namePerson)
        //{
        //    List<PersonChat> groups;
        //    using (var stream = File.Open($@"D:\temp\messenger\Users\{nickname}\peopleChatsBeen.json", FileMode.OpenOrCreate, FileAccess.Read))
        //    {
        //        var groupsJsonSB = await ReadFile(stream);
        //        groups = JsonConvert.DeserializeObject<List<PersonChat>>(groupsJsonSB.ToString());
        //    }
        //    if (groups == null || groups.Count == 0)
        //    {
        //        return new string[] { "","" };
        //    }
        //    foreach (var group in groups)
        //    {
        //        if (group.Nicknames[0] == namePerson || group.Nicknames[1] == namePerson)
        //        {
        //            return new string[] { $@"D:\temp\messenger\peopleChats\{group.NameChat}", group.NameChat};
        //        }
        //    }
        //    return new string[] { "", "" };
        //}
        private async void WriteNewPerson(string path, PersonChat personChat)
        {
            StringBuilder usersJson = new StringBuilder();
            using (var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
            {
                usersJson = await ReadFile(stream);
                var peopleChatsBeen = JsonConvert.DeserializeObject<List<PersonChat>>(usersJson.ToString());
                stream.Seek(0, SeekOrigin.Begin);
                if (peopleChatsBeen == null)
                {
                    peopleChatsBeen = new List<PersonChat>();
                }
                peopleChatsBeen.Add(personChat);
                var peopleChatsBeenJson = JsonConvert.SerializeObject(peopleChatsBeen);
                var buffer = Encoding.Default.GetBytes(peopleChatsBeenJson);
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
        private async Task<string[]> CheckChatAndCreatePath(string nameGroup, string typeGroup)
        {
            List<string> groups = new List<string>();
            var peopleChatsBeenJson = (List<PersonChat>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\peopleChatsBeen.json", false, true);
            var peopleChatsBeen = FindPeopleInChatsBeen(peopleChatsBeenJson);
            string pathGroup = "";
            var needCreatePP = false;
            var needCorect = false;
            var needAddUser = false;
            switch (typeGroup)
            {
                case "pp":
                    groups = await FindChatsPeople(peopleChatsBeen, nickname);
                    needCreatePP = true;
                    break;
                case "ch":
                    groups = peopleChatsBeen;
                    needCorect = true;
                    break;
                case "sg":
                    groups = (List<string>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\secretGroups.json", true, false);
                    pathGroup = $"{SecreatGroupsPath}\\{nameGroup}";
                    break;
                case "ug":
                    groups = (List<string>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\userGroups.json", true, false);
                    pathGroup = $"{PublicGroupsPath}\\{nameGroup}";
                    break;
                case "pg":
                    groups = FindPublicGroup();
                    pathGroup = $"{PublicGroupsPath}\\{nameGroup}";
                    needAddUser = true;
                    break;
            }
            if (groups != null && groups.Count != 0)
            {
                foreach (var group in groups)
                {
                    if (group == nameGroup)
                    {
                        if (needCorect)
                        {
                            return FindPathChat(nameGroup, peopleChatsBeenJson);
                        }
                        else if (needCreatePP)
                        {
                            return await CreateChat(nameGroup, groups);
                        }
                        else if (needAddUser)
                        {
                            await AddGroup($@"D:\temp\messenger\Users\{nickname}\userGroups.json", nameGroup, pathGroup);
                            await AddUser($"{pathGroup}\\users.json", nickname);
                        }
                        return new string[] { pathGroup, nameGroup };
                    }
                }
            }
            return new string[] { "", nameGroup };
        }
        private async Task<bool> CheckDeleteInvitation(string path, string data)
        {
            var invitations = new List<string>();
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var invitationsJsonSb = await ReadFile(stream);
                invitations = JsonConvert.DeserializeObject<List<string>>(invitationsJsonSb.ToString());
            }
            if (invitations == null)
            {
                return false;
            }
            var needDelete = invitations.Remove(data);
            if (needDelete)
            {
                var invitationJson = JsonConvert.SerializeObject(invitations);
                using (var stream = new StreamWriter(path, false))
                {
                    await stream.WriteAsync(invitationJson);
                }
            }
            return needDelete;
        }
        private async Task AddGroup(string path, string nameGroup, string pathGroup)
        {
            StringBuilder nameGroupsJsonSB = new StringBuilder();
            List<string> groups;
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                nameGroupsJsonSB = await ReadFile(stream);
                groups = JsonConvert.DeserializeObject<List<string>>(nameGroupsJsonSB.ToString());
            }
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    if (group == nameGroup)
                    {
                        return;
                    }
                }
            }
            else
            {
                groups = new List<string>();
            }
            groups.Add(nameGroup);
            var usersJson = JsonConvert.SerializeObject(groups);
            using (var stream = new StreamWriter(path, false))
            {
                await stream.WriteAsync(usersJson);
            }
            if (await CheckDeleteInvitation($@"D:\temp\messenger\Users\{nickname}\invitation.json", $"public: {nameGroup}"))
            {
                await CheckDeleteInvitation($@"{pathGroup}\invitation.json", nickname);
            }
        }
        private async Task AddUser(string path, string nick)
        {
            StringBuilder usersJsonSB = new StringBuilder();
            List<string> users;
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                usersJsonSB = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJsonSB.ToString());
            }
            if (users != null)
            {
                foreach (var user in users)
                {
                    if (user == nick)
                    {
                        return;
                    }
                }
            }
            else
            {
                users = new List<string>();
            }
            users.Add(nick);
            var usersJson = JsonConvert.SerializeObject(users);
            using (var stream = new StreamWriter(path, false))
            {
                await stream.WriteAsync(usersJson);
            }
        }
        private async Task<string[]> AcceptTheInvitation()
        {
            var invitations = (List<string>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\invitation.json", true, false);
            communication.SendMessage("If you want to join a group write: join\n\r" +
                "if you want to look at the invitation, write: look", listener);
            while (true)
            {
                communication.AnswerClient(listener);
                var message = communication.data.ToString();
                if (message == "join")
                {
                    return await JoinToGroup(invitations);
                }
                else if (message == "look")
                {
                    SendGroups(invitations, listener, "Invitation:");
                    continue;
                }
                communication.SendMessage("Bed input, write else", listener);
            }
        }
        private async Task<string[]> JoinToGroup(List<string> invitations)
        {
            communication.SendMessage("Write the name of the group", listener);
            while (true)
            {
                communication.AnswerClient(listener);
                var groupName = communication.data.ToString();
                foreach (var invitation in invitations)
                {
                    var normalInvitation = invitation.Remove(0, 8);
                    if (groupName == normalInvitation)
                    {
                        var information = await EnterTheGroup(invitation, groupName);
                        communication.SendMessage("You join the group\n\r" +
                            "If you want open chats, write: open", listener);
                        communication.AnswerClient(listener);
                        if (communication.data.ToString() == "open")
                        {
                            communication.SendMessage("You enter to the group", listener);
                            return information;
                        }
                        else
                        {
                            communication.SendMessage("Ok, bye", listener);
                            return new string[2];
                        }
                    }
                }
                communication.SendMessage("Don`t have this invitation", listener);
            }
        }
        private async Task<string[]> EnterTheGroup(string invitation, string groupName)
        {
            string typeGroup;
            string pathGroup;
            string pathUser;
            if (invitation[0] == 'p')
            {
                typeGroup = "pg";
                pathGroup = $@"{PublicGroupsPath}\{groupName}";
                pathUser = $@"D:\temp\messenger\Users\{nickname}\\userGroups.json";
            }
            else
            {
                typeGroup = "sg";
                pathGroup = $@"{SecreatGroupsPath}\{groupName}";
                pathUser = $@"D:\temp\messenger\Users\{nickname}\\secretGroups.json";
            }
            //var usersJsonSb = new StringBuilder();
            //using (var stream = File.Open($"{pathGroup}\\users.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            //{
            //    usersJsonSb = await ReadFile(stream);
            //    var users = JsonConvert.DeserializeObject<List<string>>(usersJsonSb.ToString());
            //    if (users != null)
            //    {
            //        users.Add(nickname);
            //    }
            //    else
            //    {
            //        users = new List<string>();
            //        users.Add(nickname);
            //    }
            //    var usersJson = JsonConvert.SerializeObject(users);
            //    var buffer = Encoding.Default.GetBytes(usersJson);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    await stream.WriteAsync(buffer, 0, buffer.Length);
            //}
            await WriteInvitaion($"{pathGroup}\\users.json", nickname);
            await WriteInvitaion(pathUser, groupName);
            await DeleteInvitation($@"D:\temp\messenger\Users\{nickname}\invitation.json", invitation);
            await DeleteInvitation($@"{pathGroup}\invitation.json", nickname);
            //using (var stream = File.Open($@"D:\temp\messenger\Users\{nickname}\invitation.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            //{
            //    usersJsonSb = await ReadFile(stream);
            //    var users = JsonConvert.DeserializeObject<List<string>>(usersJsonSb.ToString());
            //    users.Remove(invitation);
            //    var usersJson = JsonConvert.SerializeObject(usersJsonSb);
            //    var buffer = Encoding.Default.GetBytes(usersJson);
            //    await stream.WriteAsync(buffer, 0, buffer.Length);
            //}
            return new string[] { typeGroup, groupName, pathGroup};
        }
        private async Task WriteInvitaion(string path, string information)
        {
            var usersJsonSb = new StringBuilder();
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                usersJsonSb = await ReadFile(stream);
                var users = JsonConvert.DeserializeObject<List<string>>(usersJsonSb.ToString());
                if (users != null)
                {
                    users.Add(information);
                }
                else
                {
                    users = new List<string>();
                    users.Add(information);
                }
                var usersJson = JsonConvert.SerializeObject(users);
                var buffer = Encoding.Default.GetBytes(usersJson);
                stream.Seek(0, SeekOrigin.Begin);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        private async Task DeleteInvitation(string path, string invitation)
        {
            var users = new List<string>();
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var usersJsonSb = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJsonSb.ToString());
            }
            users.Remove(invitation);
            var usersJson = JsonConvert.SerializeObject(users);
            using (var stream = new StreamWriter(path, false))
            {
                await stream.WriteAsync(usersJson);
            }
        }
        private async Task<string[]> ChoseGroupSend(Socket listener, List<string>[] allChats, string message, string nick)
        {
            switch (message)
            {
                case "?/cc":
                    SendChats(listener, allChats, false);
                    break;
                case "?/pp":
                    SendGroups(allChats[1], listener, "All people:");
                    break;
                case "?/ch":
                    SendGroups(allChats[0], listener, "Chat with people:");
                    break;
                case "?/gg":
                    SendChats(listener, allChats, true);
                    break;
                case "?/sg":
                    SendGroups(allChats[2], listener, "Secret grout:");
                    break;
                case "?/ug":
                    SendGroups(allChats[3], listener, "User group:");
                    break;
                case "?/pg":
                    SendGroups(allChats[4], listener, "Public group:");
                    break;
                case "?/ii":
                    SendGroups(allChats[5], listener, "Invitation:");
                    break;
                case "?/ng":
                    return await CreateNewGroup(listener, nick);
                default:
                    return new string[1];
            }
            return new string[0];
        }
        private async Task<string[]> CreateNewGroup(Socket listener, string nick)
        {
            CreatorGroups creatorGroups = new CreatorGroups(listener, nick);
            return await creatorGroups.Run();
            //if (result[0] != "")
            //{
            //    return OpenCreateChat(listener, nick, result);
            //}
            //return result;
            Console.WriteLine("crasava");
            Console.ReadKey();
            //SendMessage("What type of group do you want?" +
            //    "If secret, click 's', if public, click 'p'", listener);
            //AnswerClient(listener);
            //SendMessage("Enter a group name", listener);
            //while (true)
            //{
            //    AnswerClient(listener);
            //    if (data.ToString()[0] != '?')
            //    {
            //        SendMessage("Group name can`t start with '?', enter new", listener);
            //        continue;
            //    }
            //    break;
            //}
            //SendMessage("Who do you want to invite to your group?\n\r" +
            //    "If you want to check people, write ?/yes\n\r" +
            //    "If you don`t want to add people, write ?/no\n\r", listener);
            //bool canCreate = false;
            //while (true)
            //{
            //    AnswerClient(listener);
            //    if (data.ToString() == "?/yes")
            //    {
            //        var people = await FindChatsPeople(new List<string>(), "");
            //        SendGroups(people, listener, "People");
            //    }
            //    else if (data.ToString() == "?/no")
            //    {
            //        if (canCreate)
            //        {

            //            SendMessage("You create group, thanks.\n\r" +
            //                "If you want to open it, press enter, else - press else", listener);
            //            break;
            //        }
            //        else
            //        {
            //            SendMessage("You can`t create group when you are alone in it, write other name group", listener);
            //        }
            //    }
            //    else
            //    {

            //        canCreate = true;
            //    }
            //}
        }
        private void HelpFindChat(Socket listener, List<string>[] allChats, string message)
        {
            var mode = $"{message[2]}{message[3]}";
            var nameBuilding = new StringBuilder();
            for (int i = 4; i < message.Length; i++)
            {
                nameBuilding.Append(message[i]);
            }
            var beginningName = nameBuilding.ToString();
            switch (mode)
            {
                case "cc":
                    SendChatsWithHelper(listener, 0, beginningName, new string[] { "All people:", "Chat with people:", "Secret grout:", "User group:", "Public group:", "Invitation:" }, 6, allChats);
                    break;
                case "pp":
                    SendChatsWithHelper(listener, 1, beginningName, new string[] { "All people:" }, 1, allChats);
                    break;
                case "ch":
                    SendChatsWithHelper(listener, 0, beginningName, new string[] { "Chat with people:" }, 1, allChats);
                    break;
                case "gg":
                    SendChatsWithHelper(listener, 0, beginningName, new string[] { "Secret grout:", "User group:", "Public group:", "Invitation:" }, 4, allChats);
                    break;
                case "sg":
                    SendChatsWithHelper(listener, 2, beginningName, new string[] { "Secret grout:" }, 1, allChats);
                    break;
                case "ug":
                    SendChatsWithHelper(listener, 3, beginningName, new string[] { "User group:" }, 1, allChats);
                    break;
                case "pg":
                    SendChatsWithHelper(listener, 4, beginningName, new string[] { "Public group:" }, 1, allChats);
                    break;
                case "ii":
                    SendChatsWithHelper(listener, 5, beginningName, new string[] { "Invitation:" }, 1, allChats);
                    break;
            }
        }
        private void SendChatsWithHelper(Socket listener, int numList, string beginningName, string[] firstMassages, int countChats, List<string>[] allChats)
        {
            var numArray = -1;
            for (int i = allChats.Length - countChats; i < allChats.Length; i++)
            {
                numArray++;
                List<string> needChats = new List<string>();
                if (countChats > 1)
                {
                    numList = i;
                }
                if (allChats[numList] != null)
                {
                    foreach (var chat in allChats[numList])
                    {
                        if (chat.Length >= beginningName.Length)
                        {
                            var needAdd = true;
                            for (int j = 0; j < beginningName.Length; j++)
                            {
                                if (chat[j] != beginningName[j])
                                {
                                    needAdd = false;
                                    break;
                                }
                            }
                            if (needAdd)
                            {
                                needChats.Add(chat);
                            }
                        }
                    }
                }
                SendGroups(needChats, listener, firstMassages[numArray]);
            }
        }
        private async Task<List<string>[]> FindAllChats(string nickname)
        {
            var enteringChats = await ReadFile(nickname);
            var peopleChatsJson = await FindChatsPeople(enteringChats[0], nickname);//////can be misstake
            Array.Copy(enteringChats, 0, enteringChats = new List<string>[enteringChats.Length + 1], 1, enteringChats.Length - 1);
            enteringChats[0] = enteringChats[1];
            enteringChats[1] = peopleChatsJson;
            return enteringChats;
        }
        private void SendChats(Socket listener, List<string>[] enteringChats, bool onlyGroup)
        {
            if (!onlyGroup)
            {
                SendGroups(enteringChats[0], listener, "Chat with people:");
                SendGroups(enteringChats[1], listener, "All people:");
            }
            SendGroups(enteringChats[2], listener, "Secret grout:");
            SendGroups(enteringChats[3], listener, "User group:");
            SendGroups(enteringChats[4], listener, "Public group:");
            SendGroups(enteringChats[5], listener, "Invitation:");
            //if (enteringChats[0].Count == 0)
            //{
            //    SendMessage("Chatting with people:\n\t(don`t have)", listener);
            //}
            //else
            //{
            //    SendMessage("Chatting with people:", listener);
            //    AnswerClient(listener);

            //}
            ////else
            ////{
            ////    SendMessage("SendChats", listener);
            ////    AnswerClient(listener);
            ////    ///////////////////////////////////////////////////////////sendinggoups
            ////}
            //SendPublicGroups(listener);


        }
        //private void SendSomeGroups(Socket listener, string[] group)
        //{

        //}
        //private void SendPublicGroups(Socket listener)
        //{
        //    var publicGroups = Directory.GetDirectories(PublicGroupsPath);
        //    SendMessage("Public chat", listener);
        //    AnswerClient(listener);
        //    SendMessage($"{publicGroups.Length}", listener);
        //    AnswerClient(listener);
        //    SendGroups(publicGroups, listener);
        //}
        private void SendGroups(IEnumerable<string> groups, Socket listener, string firstMassage)
        {
            communication.SendMessage(firstMassage, listener);
            communication.AnswerClient(listener);
            if (groups == null || groups.Count() == 0)
            {
                communication.SendMessage("0", listener);
                communication.AnswerClient(listener);
                return;
            }
            communication.SendMessage(groups.Count().ToString(), listener);
            communication.AnswerClient(listener);
            foreach (var group in groups)
            {
                communication.SendMessage(group, listener);
                communication.AnswerClient(listener);
            }
        }
        private async Task<List<string>[]> ReadFile(string nickname)
        {
            //var dataJson = await ReadFileToList($@"{UserFoldersPath}\{nickname}\Data.json", true); /////don`t need

            var peopleChatsBeenJson = (List<PersonChat>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\peopleChatsBeen.json", false, true);
            var peopleChatsBeen = FindPeopleInChatsBeen(peopleChatsBeenJson);
            var secretGroupsJson = (List<string>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\secretGroups.json", true, false);
            var userGroupsJson = (List<string>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\userGroups.json", true, false);
            var publicGroups = FindPublicGroup();
            var invitationJson = (List<string>)await ReadFileToList($@"{UserFoldersPath}\{nickname}\invitation.json", true, false);
            return new List<string>[] { peopleChatsBeen, secretGroupsJson, userGroupsJson, publicGroups, invitationJson };
        }
        private List<string> FindPeopleInChatsBeen(List<PersonChat> peopleChatsBeenJson)
        {
            if (peopleChatsBeenJson == null || peopleChatsBeenJson.Count == 0)
            {
                return new List<string>();
            }
            var peopleChatsBeen = new List<string>();
            foreach (var peopleChatBeenJson in peopleChatsBeenJson)
            {
                if (peopleChatBeenJson.Nicknames[0] != nickname)
                {
                    peopleChatsBeen.Add(peopleChatBeenJson.Nicknames[0]);
                }
                else
                {
                    peopleChatsBeen.Add(peopleChatBeenJson.Nicknames[1]);
                }
            }
            return peopleChatsBeen;
        }
        private async Task<object> ReadFileToList(string path, bool toString, bool toPersonChat)
        {
            StringBuilder usersJson = new StringBuilder();
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
            {
                usersJson = await ReadFile(stream);
            }
            if (toString)
            {
                return JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
            else if (toPersonChat)
            {
                return JsonConvert.DeserializeObject<List<PersonChat>>(usersJson.ToString());
            }
            else
            {
                return JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
            }
        }
        private List<string> FindPublicGroup()
        {
            var publicGroups = Directory.GetDirectories(@"D:\temp\messenger\publicGroup");
            var publicGroupList = new List<string>();
            foreach (var publicGroup in publicGroups)
            {
                publicGroupList.Add(Path.GetFileName(publicGroup));
            }
            return publicGroupList;
        }
        private async Task<List<string>> FindChatsPeople(List<string> peopleChatsBeenJson, string userNick)
        {
            var allPeopleJson = (List<UserNicknameAndPasswordAndIPs>)await ReadFileToList($@"D:\temp\messenger\nicknamesAndPasswords\users.json", false, false);
            //var allPeopleWithoutPasswordJson = allPeopleJson.Select(x => x.Nickname).Where(x => x != userNick);
            var allPeopleWithoutPasswordJsons = new List<string>();
            if (allPeopleJson != null)
            {
                foreach (var personJson in allPeopleJson)
                {
                    var needAdd = true;
                    if (peopleChatsBeenJson != null)
                    {
                        foreach (var personChatBeenJson in peopleChatsBeenJson)
                        {
                            if (personJson.Nickname == personChatBeenJson)
                            {
                                needAdd = false;
                                continue;
                            }
                        }
                    }
                    if (needAdd)
                    {
                        if (personJson.Nickname != userNick)
                        {
                            allPeopleWithoutPasswordJsons.Add(personJson.Nickname);
                        }
                    }
                }
            }
            return allPeopleWithoutPasswordJsons;
            //if (allPeopleWithoutPasswordJson.Count() != 0)
            //{
            //    if (peopleChatsBeenJson != null)
            //    {
            //        return allPeopleWithoutPasswordJson.Except(peopleChatsBeenJson);
            //    }
            //    return allPeopleWithoutPasswordJson;
            //}
            //return peopleChatsBeenJson;
        }
        
    }
}
