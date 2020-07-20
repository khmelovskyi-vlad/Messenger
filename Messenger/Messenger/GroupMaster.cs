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
        private FileMaster fileMaster = new FileMaster();
        private UserChats userChats;
        public async Task<GroupInformation> Run()
        {
            userChats = new UserChats(fileMaster, user.Nickname, UserFoldersPath, PublicGroupsPath, SecreatGroupsPath, PeopleChatsPath);
            await userChats.FindAllChats();
            //var enteringChats = await FindAllChats();
            SendChats(false);
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
                    return await ModeSelection();
            //    }
            //}
        }
        private async Task<GroupInformation> ModeSelection()
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
                        var groupInformation = await ChoseGroupSend(message);
                        if (groupInformation.Name != null)
                        {
                            return groupInformation;
                        }
                    }
                    else
                    {
                        HelpFindChat(message);
                    }
                }
                else
                {
                    if (message == "exit")
                    {
                        SendMessage("You exit messanger");
                        return new GroupInformation { CanOpenChat = false };
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
        }
        private async Task<GroupInformation> FindNeedGroup(string message, string typeGroup)
        {
            GroupInformation groupInformation;
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
                groupInformation = await CheckChatAndCreatePath(user.communication.data.ToString(), typeGroup);
                if (groupInformation.CanOpenChat)
                {
                    SendMessage("You connect to chat");
                    return groupInformation;
                }
                SendMessage("Don`t have this chat, enter new please");
            }
        }
        private async Task<GroupInformation> FindPathChat(string namePerson, string typeGroup)
        {
            var personChats = await userChats.FindPersonChats();
            foreach (var personChat in personChats)
            {
                if (personChat.Nicknames[0] == namePerson || personChat.Nicknames[1] == namePerson)
                {
                    return new GroupInformation(true, typeGroup,
                        personChat.NameChat,
                        $@"D:\temp\messenger\peopleChats\{personChat.NameChat}");
                }
            }
            return new GroupInformation { CanOpenChat = false};
        }
        private async Task<GroupInformation> CreateChat(string namePerson, List<string> people, string typeGroup)
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
            return new GroupInformation(true, typeGroup, nameChat, path);
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
        private async Task<GroupInformation> CheckChatAndCreatePath(string nameGroup, string typeGroup)
        {
            List<string> groups = new List<string>();
            string pathGroup = $"{PublicGroupsPath}\\{nameGroup}";
            var needCreatePP = false;
            var needCorect = false;
            var needAddUser = false;
            switch (typeGroup)
            {
                case "pp":
                    groups = userChats.AllElsePeople;
                    needCreatePP = true;
                    break;
                case "ch":
                    groups = userChats.ChatsWithPeople;
                    needCorect = true;
                    break;
                case "sg":
                    groups = userChats.SecretGroups;
                    pathGroup = $"{SecreatGroupsPath}\\{nameGroup}";
                    break;
                case "ug":
                    groups = userChats.UserGroups;
                    break;
                case "pg":
                    groups = userChats.PublicGroups;
                    needAddUser = true;
                    break;
            }
            if (groups != null/* && groups.Count != 0 */)
            {
                foreach (var group in groups)
                {
                    if (group == nameGroup)
                    {
                        if (needCreatePP)
                        {
                            return await CreateChat(nameGroup, groups, typeGroup);
                        }
                        else if (needCorect)
                        {
                            return await FindPathChat(nameGroup, typeGroup);
                        }
                        else if (needAddUser)
                        {
                            await AddGroup($@"D:\temp\messenger\Users\{user.Nickname}\userGroups.json", nameGroup, pathGroup);
                        }
                        return new GroupInformation(true, typeGroup, nameGroup, pathGroup);
                    }
                }
            }
            return new GroupInformation(false, typeGroup, nameGroup, pathGroup);
        }
        private async Task<bool> CheckDeleteInvitation(string path, string data)
        {
            return await fileMaster.ReadWrite(path, invitations =>
            {
                if (invitations == null)
                {
                    return (invitations, false);
                }
                return (invitations, invitations.Remove(data));
            });
        }
        private async Task AddGroup(string path, string nameGroup, string pathGroup)
        {
            await AddData(path, nameGroup);
            if (await CheckDeleteInvitation($@"D:\temp\messenger\Users\{user.Nickname}\invitation.json", $"public: {nameGroup}"))
            {
                await CheckDeleteInvitation($@"{pathGroup}\invitation.json", user.Nickname);
            }
            await AddData($"{pathGroup}\\users.json", user.Nickname);
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
                    if (allData.Contains(data))
                    {
                        return (allData, false);
                    }
                }
                allData.Add(data);
                return (allData, true);
            });
        }
        private async Task<GroupInformation> AcceptTheInvitation()
        {
            await userChats.FindInvitations();
            SendMessage("If you want to join a group write: join\n\r" +
                "if you want to look at the invitations, write: look");
            while (true)
            {
                AnswerClient();
                var message = user.communication.data.ToString();
                switch (message)
                {
                    case "join":
                        return await JoinToGroup();
                    case "look":
                        SendGroups(userChats.Invitations, "Invitation:");
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
        private async Task<GroupInformation> JoinToGroup()
        {
            SendMessage("Write the name of the group");
            while (true)
            {
                AnswerClient();
                var groupName = user.communication.data.ToString();
                foreach (var invitation in userChats.Invitations)
                {
                    var normalInvitation = invitation.Remove(0, 8);
                    if (groupName == normalInvitation)
                    {
                        var groupInformation = await EnterTheGroup(invitation, groupName);
                        SendMessage("You have joined to the group\n\r" +
                            "If you want to open chats, write: 'open'");
                        AnswerClient();
                        if (user.communication.data.ToString() == "open")
                        {
                            SendMessage("You enter to the group");
                            return groupInformation;
                        }
                        else
                        {
                            SendMessage("Ok, bye");
                            return new GroupInformation { CanOpenChat = false };
                        }
                    }
                }
                SendMessage("Don`t have this invitation");
            }
        }
        private async Task<GroupInformation> EnterTheGroup(string invitation, string nameGroup)
        {
            string typeGroup;
            string pathGroup;
            string pathUser;
            switch (invitation[0])
            {
                case 'p':
                    typeGroup = "pg";
                    pathGroup = $@"{PublicGroupsPath}\{nameGroup}";
                    pathUser = $@"D:\temp\messenger\Users\{user.Nickname}\\userGroups.json";
                    break;
                default: //(sg)
                    typeGroup = "sg";
                    pathGroup = $@"{SecreatGroupsPath}\{nameGroup}";
                    pathUser = $@"D:\temp\messenger\Users\{user.Nickname}\\secretGroups.json";
                    break;
            }
            await AddUserOrGroup($"{pathGroup}\\users.json", user.Nickname);
            await AddUserOrGroup(pathUser, nameGroup);
            await DeleteInvitation($@"D:\temp\messenger\Users\{user.Nickname}\invitation.json", invitation);
            await DeleteInvitation($@"{pathGroup}\invitation.json", user.Nickname);
            return new GroupInformation(true, typeGroup, nameGroup, pathGroup);
        }
        private async Task AddUserOrGroup(string path, string information)
        {
            await fileMaster.ReadWrite(path, usersOrGroup =>
            {
                if (usersOrGroup == null)
                {
                    usersOrGroup = new List<string>();
                }
                usersOrGroup.Add(information);
                return (usersOrGroup, true);
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
        private async Task<GroupInformation> ChoseGroupSend(string message)
        {
            switch (message)
            {
                case "?/cc":
                    SendChats(false);
                    break;
                case "?/pp":
                    SendGroups(userChats.AllElsePeople, "All people:");
                    break;
                case "?/ch":
                    SendGroups(userChats.ChatsWithPeople, "Chat with people:");
                    break;
                case "?/gg":
                    SendChats(true);
                    break;
                case "?/sg":
                    SendGroups(userChats.SecretGroups, "Secret grout:");
                    break;
                case "?/ug":
                    SendGroups(userChats.UserGroups, "User group:");
                    break;
                case "?/pg":
                    SendGroups(userChats.PublicGroups, "Public group:");
                    break;
                case "?/ii":
                    SendGroups(userChats.Invitations, "Invitation:");
                    break;
                case "?/ng":
                    return await CreateNewGroup();
                default:
                    return new GroupInformation() { CanOpenChat = false };
            }
            return new GroupInformation() { CanOpenChat = false };
        }
        private async Task<GroupInformation> CreateNewGroup()
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
        private void HelpFindChat(string message)
        {
            var mode = message.Substring(2, 2);
            var beginningName = message.Remove(0, 4);
            switch (mode)
            {
                case "cc":
                    SendChatsWithHelper(new List<List<string>> { userChats.AllElsePeople, userChats.ChatsWithPeople, userChats.SecretGroups,
                    userChats.UserGroups, userChats.PublicGroups, userChats.Invitations},
                    new string[] { "All people:", "Chats with people:", "Secret groups:", "User groups:", "Public groups:", "Invitations:" },
                    beginningName);
                    //SendChatsWithHelper(0, beginningName, new string[] { "All people:", "Chat with people:", "Secret grout:", "User group:", "Public group:", "Invitation:" }, 6, allChats);
                    break;
                case "pp":
                    SendChatsWithHelper(new List<List<string>> { userChats.AllElsePeople}, new string[] { "All people:" }, beginningName);
                    //SendChatsWithHelper(1, beginningName, new string[] { "All people:" }, 1, allChats);
                    break;
                case "ch":
                    SendChatsWithHelper(new List<List<string>> { userChats.ChatsWithPeople }, new string[] { "Chats with people:" }, beginningName);
                    //SendChatsWithHelper(0, beginningName, new string[] { "Chat with people:" }, 1, allChats);
                    break;
                case "gg":
                    SendChatsWithHelper(new List<List<string>> { userChats.SecretGroups, userChats.UserGroups, userChats.PublicGroups, userChats.Invitations},
                    new string[] { "Secret groups:", "User groups:", "Public groups:", "Invitations:" }, beginningName);
                    //SendChatsWithHelper(0, beginningName, new string[] { "Secret grout:", "User group:", "Public group:", "Invitation:" }, 4, allChats);
                    break;
                case "sg":
                    SendChatsWithHelper(new List<List<string>> { userChats.SecretGroups }, new string[] { "Secret groups:" }, beginningName);
                    //SendChatsWithHelper(2, beginningName, new string[] { "Secret grout:" }, 1, allChats);
                    break;
                case "ug":
                    SendChatsWithHelper(new List<List<string>> { userChats.UserGroups }, new string[] { "User groups:" }, beginningName);
                    //SendChatsWithHelper(3, beginningName, new string[] { "User group:" }, 1, allChats);
                    break;
                case "pg":
                    SendChatsWithHelper(new List<List<string>> { userChats.PublicGroups }, new string[] { "Public groups:" }, beginningName);
                    //SendChatsWithHelper(4, beginningName, new string[] { "Public group:" }, 1, allChats);
                    break;
                case "ii":
                    SendChatsWithHelper(new List<List<string>> { userChats.Invitations }, new string[] { "Invitations:" }, beginningName);
                    //SendChatsWithHelper(5, beginningName, new string[] { "Invitation:" }, 1, allChats);
                    break;
            }
        }
        private void SendChatsWithHelper(List<List<string>> chats, string[] firstMessages, string beginningName)
        {
            var i = 0;
            foreach (var chat in chats)
            {
                SendGroups((chat ?? new List<string>()).Where(x => x.Substring(0, beginningName.Length) == beginningName), firstMessages[i]);
                i++;
            }
        }
        //private void SendChatsWithHelper(int numList, string beginningName, string[] firstMassages, int countChats, List<string>[] allChats)
        //{
        //    var numArray = -1;
        //    for (int i = allChats.Length - countChats; i < allChats.Length; i++)
        //    {
        //        numArray++;
        //        List<string> needChats = new List<string>();
        //        if (countChats > 1)
        //        {
        //            numList = i;
        //        }
        //        if (allChats[numList] != null)
        //        {
        //            foreach (var chat in allChats[numList])
        //            {
        //                if (chat.Length >= beginningName.Length)
        //                {
        //                    var needAdd = true;
        //                    for (int j = 0; j < beginningName.Length; j++)
        //                    {
        //                        if (chat[j] != beginningName[j])
        //                        {
        //                            needAdd = false;
        //                            break;
        //                        }
        //                    }
        //                    if (needAdd)
        //                    {
        //                        needChats.Add(chat);
        //                    }
        //                }
        //            }
        //        }
        //        SendGroups(needChats, firstMassages[numArray]);
        //    }
        //}
        private void SendChats(bool onlyGroup)
        {
            if (!onlyGroup)
            {
                SendGroups(userChats.ChatsWithPeople, "Chats with people:");
                SendGroups(userChats.AllElsePeople, "All people:");
            }
            SendGroups(userChats.SecretGroups, "Secret groups:");
            SendGroups(userChats.UserGroups, "User groups:");
            SendGroups(userChats.PublicGroups, "Public groups:");
            SendGroups(userChats.Invitations, "Invitations:");
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
        //private async Task<List<string>[]> FindAllChats()
        //{
        //    return await ReadFiles();
        //    //var peopleChatsJson = await FindChatsPeople(enteringChats[0]);//////can be misstake
        //    //Array.Copy(enteringChats, 0, enteringChats = new List<string>[enteringChats.Length + 1], 1, enteringChats.Length - 1);
        //    //enteringChats[0] = enteringChats[1];
        //    //enteringChats[1] = peopleChatsJson;
        //    //return enteringChats;
        //}
        private async Task<List<string>[]> ReadFiles()
        {

            var peopleChatsBeenJson = await fileMaster.ReadAndDesToPersonCh($@"{UserFoldersPath}\{user.Nickname}\peopleChatsBeen.json");
            var peopleChatsBeen = FindPeopleInChatsBeen(peopleChatsBeenJson);
            //var peopleChatsBeen = (await fileMaster.ReadAndDesToPersonCh($@"{UserFoldersPath}\{user.Nickname}\peopleChatsBeen.json"))
            //    .Select(chat => chat.Nicknames[0] != user.Nickname ? chat.Nicknames[0] : chat.Nicknames[1])
            //    .DefaultIfEmpty()
            //    .ToList();
            var peopleChatsJson = await FindChatsPeople(peopleChatsBeen);//////can be misstake
            var peopleChatsJsonn = (await fileMaster.ReadAndDesToLUserInf($@"D:\temp\messenger\nicknamesAndPasswords\users.json"))
                .Select(x => x.Nickname)
                .Where(x => x != user.Nickname)
                .Except(peopleChatsBeen)
                .DefaultIfEmpty()
                .ToList();
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
