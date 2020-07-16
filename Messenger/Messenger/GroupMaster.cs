using Newtonsoft.Json;
using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class GroupMaster
    {
        public GroupMaster(User user)
        {
            this.user = user;
        }
        private User user;
        private string UserFoldersPath { get { return @"D:\temp\messenger\Users"; } }
        private string PublicGroupsPath { get { return @"D:\temp\messenger\publicGroup"; } }
        private string SecreatGroupsPath { get { return @"D:\temp\messenger\secretGroup"; } }
        private string PeopleChatsPath { get { return @"D:\temp\messenger\peopleChats"; } }
        FileMaster fileMaster = new FileMaster();
        public async Task<string[]> Run()
        {
            var enteringChats = await FindAllChats();
            SendChats(enteringChats, false);
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
                    return await ModeSelection(enteringChats);
            //    }
            //}
        }
        private async Task<string[]> ModeSelection(List<string>[] allChats)
        {
            while (true)
            {
                SendMessage("Enter type of chat, what do you need\n\r" +
                    "pp - people chat, ch - created chat with people, sg - secret group, ug - user group, pg - public group\n\r" +
                    "ii - accept the invitation\n\r" +
                    "Helper:\n\r" +
                    "Write all chats: ?/cc, find chat: ?/cc...\n\r" +
                    "Write all people: ?/pp, find people: ?/pp...\n\r" +
                    "Write all chats with people: ?/ch, find chat with people: ?/ch...\n\r" +
                    "Write all groups: ?/gg, find group: ?/gg...\n\r" +
                    "Write all secret groups: ?/sg, find secreat group: ?/sg...\n\r" +
                    "Write all user groups: ?/ug, find user group: ?/ug...\n\r" +
                    "Write all public groups: ?/pg, find public group: ?/pg...\n\r" +
                    "Write all invitations: ?/ii, find invitation: ?/ii...\n\r" +
                    "If you want create new group, write ?/ng\n\r" +
                    "If you want exit, write: exit\n\r");
                AnswerClient();
                var message = user.communication.data.ToString();
                if (message.Length > 3 && message[0] == '?' && message[1] == '/')
                {
                    SendMessage("Ok");
                    AnswerClient();
                    if (message.Length == 4)
                    {
                        var information = await ChoseGroupSend(allChats, message);
                        if (information.Length == 3)
                        {
                            return information;
                        }
                        continue;
                    }
                    else
                    {
                        HelpFindChat(allChats, message);
                    }
                }
                else
                {
                    if (message == "exit")
                    {
                        SendMessage("You exit messanger");
                        return new string[0];
                    }
                    else if(message == "ii")
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
            string[] pathAndName;
            if (typeGroup == "ch" || typeGroup == "pp")
            {
                SendMessage("Enter user name");
            }
            else
            {
                SendMessage("Enter name of chat");
            }
            while (true)
            {
                AnswerClient();
                pathAndName = await CheckChatAndCreatePath(user.communication.data.ToString(), typeGroup);
                if (pathAndName[0] != "")
                {
                    SendMessage("You connect to chat");
                    pathGroup = pathAndName[0];
                    nameGroup = pathAndName[1];
                    break;
                }
                SendMessage("Don`t have this chat, enter new please");
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
            var nameChat = $"{namePerson} {user.Nickname}";
            PersonChat personChat = new PersonChat(new string[] { namePerson, user.Nickname }, nameChat);
            WriteNewPerson($@"D:\temp\messenger\Users\{user.Nickname}\peopleChatsBeen.json", personChat);
            WriteNewPerson($@"D:\temp\messenger\Users\{namePerson}\peopleChatsBeen.json", personChat);
            var path = $@"D:\temp\messenger\peopleChats\{nameChat}";
            fileMaster.CreateDirectory(path);
            await AddData($"{path}\\users.json", user.Nickname);
            await AddData($"{path}\\users.json", namePerson);
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
            await fileMaster.ReadWrite(path, peopleChatsBeen =>
            {
                if (peopleChatsBeen == null)
                {
                    peopleChatsBeen = new List<PersonChat>();
                }
                peopleChatsBeen.Add(personChat);
                return (peopleChatsBeen, true);
            });
        }
        private async Task<string[]> CheckChatAndCreatePath(string nameGroup, string typeGroup)
        {
            List<string> groups = new List<string>();
            var peopleChatsBeenJson = await fileMaster.ReadAndDesToPersonCh($@"{UserFoldersPath}\{user.Nickname}\peopleChatsBeen.json");
            var peopleChatsBeen = FindPeopleInChatsBeen(peopleChatsBeenJson);
            string pathGroup = "";
            var needCreatePP = false;
            var needCorect = false;
            var needAddUser = false;
            switch (typeGroup)
            {
                case "pp":
                    groups = await FindChatsPeople(peopleChatsBeen);
                    needCreatePP = true;
                    break;
                case "ch":
                    groups = peopleChatsBeen;
                    needCorect = true;
                    break;
                case "sg":
                    groups = await fileMaster.ReadAndDesToLString($@"{UserFoldersPath}\{user.Nickname}\secretGroups.json");
                    pathGroup = $"{SecreatGroupsPath}\\{nameGroup}";
                    break;
                case "ug":
                    groups = await fileMaster.ReadAndDesToLString($@"{UserFoldersPath}\{user.Nickname}\userGroups.json");
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
                            await AddGroup($@"D:\temp\messenger\Users\{user.Nickname}\userGroups.json", nameGroup, pathGroup);
                            await AddData($"{pathGroup}\\users.json", user.Nickname);
                        }
                        return new string[] { pathGroup, nameGroup };
                    }
                }
            }
            return new string[] { "", nameGroup };
        }
        private async Task<bool> CheckDeleteInvitation(string path, string data)
        {
            var needDelete = false;
            await fileMaster.ReadWrite(path, invitations =>
            {
                if (invitations == null)
                {
                    return (invitations, false);
                }
                needDelete = invitations.Remove(data);
                return (invitations, needDelete);
            });
            return needDelete;
        }
        private async Task AddGroup(string path, string nameGroup, string pathGroup)
        {
            await AddData(path, nameGroup);
            if (await CheckDeleteInvitation($@"D:\temp\messenger\Users\{user.Nickname}\invitation.json", $"public: {nameGroup}"))
            {
                await CheckDeleteInvitation($@"{pathGroup}\invitation.json", user.Nickname);
            }
        }
        private async Task AddData(string path, string data)
        {
            await fileMaster.ReadWrite(path, allData =>
            {
                if (allData == null)
                {
                    allData = new List<string>();
                }
                else
                {
                    foreach (var user in allData)
                    {
                        if (user == data)
                        {
                            return (allData, false);
                        }
                    }
                }
                allData.Add(data);
                return (allData, true);
            });
        }
        private async Task<string[]> AcceptTheInvitation()
        {
            var invitations = await fileMaster.ReadAndDesToLString($@"{UserFoldersPath}\{user.Nickname}\invitation.json");
            SendMessage("If you want to join a group write: join\n\r" +
                "if you want to look at the invitations, write: look");
            while (true)
            {
                AnswerClient();
                var message = user.communication.data.ToString();
                switch (message)
                {
                    case "join":
                        return await JoinToGroup(invitations);
                    case "look":
                        SendGroups(invitations, "Invitation:");
                        break;
                    default:
                        SendMessage("Bed input, write else");
                        break;
                }
                //if (message == "join")
                //{
                //    return await JoinToGroup(invitations);
                //}
                //else if (message == "look")
                //{
                //    SendGroups(invitations, "Invitation:");
                //}
                //else
                //{
                //    SendMessage("Bed input, write else");
                //}
            }
        }
        private async Task<string[]> JoinToGroup(List<string> invitations)
        {
            SendMessage("Write the name of the group");
            while (true)
            {
                AnswerClient();
                var groupName = user.communication.data.ToString();
                foreach (var invitation in invitations)
                {
                    var normalInvitation = invitation.Remove(0, 8);
                    if (groupName == normalInvitation)
                    {
                        var information = await EnterTheGroup(invitation, groupName);
                        SendMessage("You have joined to the group\n\r" +
                            "If you want to open chats, write: 'open'");
                        AnswerClient();
                        if (user.communication.data.ToString() == "open")
                        {
                            SendMessage("You enter to the group");
                            return information;
                        }
                        else
                        {
                            SendMessage("Ok, bye");
                            return new string[2];
                        }
                    }
                }
                SendMessage("Don`t have this invitation");
            }
        }
        private async Task<string[]> EnterTheGroup(string invitation, string groupName)
        {
            string typeGroup;
            string pathGroup;
            string pathUser;
            switch (invitation[0])
            {
                case 'p':
                    typeGroup = "pg";
                    pathGroup = $@"{PublicGroupsPath}\{groupName}";
                    pathUser = $@"D:\temp\messenger\Users\{user.Nickname}\\userGroups.json";
                    break;
                default:
                    typeGroup = "sg";
                    pathGroup = $@"{SecreatGroupsPath}\{groupName}";
                    pathUser = $@"D:\temp\messenger\Users\{user.Nickname}\\secretGroups.json";
                    break;
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
            await WriteInvitaion($"{pathGroup}\\users.json", user.Nickname);
            await WriteInvitaion(pathUser, groupName);
            await DeleteInvitation($@"D:\temp\messenger\Users\{user.Nickname}\invitation.json", invitation);
            await DeleteInvitation($@"{pathGroup}\invitation.json", user.Nickname);
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
            await fileMaster.ReadWrite(path, users =>
            {
                if (users == null)
                {
                    users = new List<string>();
                }
                users.Add(information);
                return (users, true);
            });
        }
        private async Task DeleteInvitation(string path, string invitation)
        {
            await fileMaster.ReadWrite(path, users =>
            {
                users.Remove(invitation);
                return (users, true);
            });
        }
        private async Task<string[]> ChoseGroupSend(List<string>[] allChats, string message)
        {
            switch (message)
            {
                case "?/cc":
                    SendChats(allChats, false);
                    break;
                case "?/pp":
                    SendGroups(allChats[1], "All people:");
                    break;
                case "?/ch":
                    SendGroups(allChats[0], "Chat with people:");
                    break;
                case "?/gg":
                    SendChats(allChats, true);
                    break;
                case "?/sg":
                    SendGroups(allChats[2], "Secret grout:");
                    break;
                case "?/ug":
                    SendGroups(allChats[3], "User group:");
                    break;
                case "?/pg":
                    SendGroups(allChats[4], "Public group:");
                    break;
                case "?/ii":
                    SendGroups(allChats[5], "Invitation:");
                    break;
                case "?/ng":
                    return await CreateNewGroup();
                default:
                    return new string[1];
            }
            return new string[0];
        }
        private async Task<string[]> CreateNewGroup()
        {
            CreatorGroups creatorGroups = new CreatorGroups(user);
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
        private void HelpFindChat(List<string>[] allChats, string message)
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
                    SendChatsWithHelper(0, beginningName, new string[] { "All people:", "Chat with people:", "Secret grout:", "User group:", "Public group:", "Invitation:" }, 6, allChats);
                    break;
                case "pp":
                    SendChatsWithHelper(1, beginningName, new string[] { "All people:" }, 1, allChats);
                    break;
                case "ch":
                    SendChatsWithHelper(0, beginningName, new string[] { "Chat with people:" }, 1, allChats);
                    break;
                case "gg":
                    SendChatsWithHelper(0, beginningName, new string[] { "Secret grout:", "User group:", "Public group:", "Invitation:" }, 4, allChats);
                    break;
                case "sg":
                    SendChatsWithHelper(2, beginningName, new string[] { "Secret grout:" }, 1, allChats);
                    break;
                case "ug":
                    SendChatsWithHelper(3, beginningName, new string[] { "User group:" }, 1, allChats);
                    break;
                case "pg":
                    SendChatsWithHelper(4, beginningName, new string[] { "Public group:" }, 1, allChats);
                    break;
                case "ii":
                    SendChatsWithHelper(5, beginningName, new string[] { "Invitation:" }, 1, allChats);
                    break;
            }
        }
        private void SendChatsWithHelper(int numList, string beginningName, string[] firstMassages, int countChats, List<string>[] allChats)
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
                SendGroups(needChats, firstMassages[numArray]);
            }
        }
        private void SendChats(List<string>[] enteringChats, bool onlyGroup)
        {
            if (!onlyGroup)
            {
                SendGroups(enteringChats[0], "Chats with people:");
                SendGroups(enteringChats[1], "All people:");
            }
            SendGroups(enteringChats[2], "Secret groups:");
            SendGroups(enteringChats[3], "User groups:");
            SendGroups(enteringChats[4], "Public groups:");
            SendGroups(enteringChats[5], "Invitations:");
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
        private void SendGroups(IEnumerable<string> groups, string firstMassage)
        {
            SendMessage(firstMassage);
            AnswerClient();
            if (groups == null || groups.Count() == 0)
            {
                SendMessage("0");
                AnswerClient();
                return;
            }
            SendMessage(groups.Count().ToString());
            AnswerClient();
            foreach (var group in groups)
            {
                SendMessage(group);
                AnswerClient();
            }
        }
        private async Task<List<string>[]> FindAllChats()
        {
            return await ReadFiles();
            //var peopleChatsJson = await FindChatsPeople(enteringChats[0]);//////can be misstake
            //Array.Copy(enteringChats, 0, enteringChats = new List<string>[enteringChats.Length + 1], 1, enteringChats.Length - 1);
            //enteringChats[0] = enteringChats[1];
            //enteringChats[1] = peopleChatsJson;
            //return enteringChats;
        }
        private async Task<List<string>[]> ReadFiles()
        {
            //var dataJson = await ReadFileToList($@"{UserFoldersPath}\{nickname}\Data.json", true); /////don`t need

            var peopleChatsBeenJson = await fileMaster.ReadAndDesToPersonCh($@"{UserFoldersPath}\{user.Nickname}\peopleChatsBeen.json");
            var peopleChatsBeen = FindPeopleInChatsBeen(peopleChatsBeenJson);
            //var peopleChatsBeen = (await fileMaster.ReadAndDesToPersonCh($@"{UserFoldersPath}\{user.Nickname}\peopleChatsBeen.json"))
            //    .Select(chat => chat.Nicknames[0] != user.Nickname ? chat.Nicknames[0] : chat.Nicknames[1])
            //    .DefaultIfEmpty()
            //    .ToList();
            var peopleChatsJson = await FindChatsPeople(peopleChatsBeen);//////can be misstake
            //var peopleChatsJson = (await fileMaster.ReadAndDesToLUserInf($@"D:\temp\messenger\nicknamesAndPasswords\users.json"))
            //    .Select(x => x.Nickname)
            //    .Where(x => x != user.Nickname)
            //    .Distinct()
            //    .DefaultIfEmpty()
            //    .ToList();
            var secretGroupsJson = await fileMaster.ReadAndDesToLString($@"{UserFoldersPath}\{user.Nickname}\secretGroups.json");
            var userGroupsJson = await fileMaster.ReadAndDesToLString($@"{UserFoldersPath}\{user.Nickname}\userGroups.json");
            var publicGroups = FindPublicGroup();
            //var publicGroups = fileMaster.GetDirectories(@"D:\temp\messenger\publicGroup")
            //    .Select(path => fileMaster.GetFileName(path))
            //    .DefaultIfEmpty()
            //    .ToList();
            var invitationJson = await fileMaster.ReadAndDesToLString($@"{UserFoldersPath}\{user.Nickname}\invitation.json");
            return new List<string>[] { peopleChatsBeen, peopleChatsJson, secretGroupsJson, userGroupsJson, publicGroups, invitationJson };
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
                if (peopleChatBeenJson.Nicknames[0] != user.Nickname)
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
        //private async Task<object> ReadFileToList(string path, bool toString, bool toPersonChat)
        //{
        //    StringBuilder usersJson = new StringBuilder();
        //    using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
        //    {
        //        usersJson = await ReadFile(stream);
        //    }
        //    if (toString)
        //    {
        //        return JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
        //    }
        //    else if (toPersonChat)
        //    {
        //        return JsonConvert.DeserializeObject<List<PersonChat>>(usersJson.ToString());
        //    }
        //    else
        //    {
        //        return JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(usersJson.ToString());
        //    }
        //}
        private List<string> FindPublicGroup()
        {
            var publicGroups = fileMaster.GetDirectories(@"D:\temp\messenger\publicGroup");
            var publicGroupList = new List<string>();
            foreach (var publicGroup in publicGroups)
            {
                publicGroupList.Add(fileMaster.GetFileName(publicGroup));
            }
            return publicGroupList;
        }
        private async Task<List<string>> FindChatsPeople(List<string> peopleChatsBeenJson)
        {
            var allPeopleJson = await fileMaster.ReadAndDesToLUserInf($@"D:\temp\messenger\nicknamesAndPasswords\users.json");
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
                                continue; //break?
                            }
                        }
                    }
                    if (needAdd)
                    {
                        if (personJson.Nickname != user.Nickname)
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
        private void SendMessage(string message)
        {
            user.communication.SendMessage(message);
        }
        private void AnswerClient()
        {
            user.communication.AnswerClient();
        }

    }
}
