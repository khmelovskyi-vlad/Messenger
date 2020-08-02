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
            await SendChats(false);
            return await ModeSelection();
        }
        private async Task<GroupInformation> ModeSelection()
        {
            while (true)
            {
                await SendMessage($"Enter type of chat, what do you need{Environment.NewLine}" +
                    $"pp - people chat, ch - created chat with people, sg - secret group, ug - user group, pg - public group{Environment.NewLine}" +
                    $"ii - accept the invitation{Environment.NewLine}" +
                    $"Helper:{Environment.NewLine}" +
                    $"Write all chats: ?/cc, find chat: ?/cc...{Environment.NewLine}" +
                    $"Write all people: ?/pp, find people: ?/pp...{Environment.NewLine}" +
                    $"Write all chats with people: ?/ch, find chat with people: ?/ch...{Environment.NewLine}" +
                    $"Write all groups: ?/gg, find group: ?/gg...{Environment.NewLine}" +
                    $"Write all secret groups: ?/sg, find secreat group: ?/sg...{Environment.NewLine}" +
                    $"Write all user groups: ?/ug, find user group: ?/ug...{Environment.NewLine}" +
                    $"Write all public groups: ?/pg, find public group: ?/pg...{Environment.NewLine}" +
                    $"Write all invitations: ?/ii, find invitation: ?/ii...{Environment.NewLine}" +
                    $"If you want create new group, write ?/ng{Environment.NewLine}" +
                    $"If you want exit, write: exit{Environment.NewLine}");
                await AnswerClient();
                var message = user.communication.data.ToString();
                if (message.Length > 3 && message[0] == '?' && message[1] == '/')
                {
                    await SendMessage("Ok");
                    await AnswerClient();
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
                        await HelpFindChat(message);
                    }
                }
                else
                {
                    if (message == "exit")
                    {
                        await SendMessage("You exit messanger");
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
                await SendMessage("Enter user name");
            }
            else
            {
                await SendMessage("Enter name of chat");
            }
            while (true)
            {
                await AnswerClient();
                groupInformation = await CheckChatAndCreatePath(user.communication.data.ToString(), typeGroup);
                if (groupInformation.CanOpenChat)
                {
                    await SendMessage("You connect to chat");
                    return groupInformation;
                }
                await SendMessage("Don`t have this chat, enter new please");
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
        private async Task<GroupInformation> CreateChat(string namePerson, string typeGroup)
        {
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
        private async void WriteNewPerson(string path, PersonChat personChat)
        {
            await fileMaster.UpdateFile(path, fileMaster.AddData(personChat));
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
                    groups = await userChats.FindAllElsePeople();
                    needCreatePP = true;
                    break;
                case "ch":
                    groups = await userChats.FindChatsWithPeople();
                    needCorect = true;
                    break;
                case "sg":
                    groups = await userChats.FindSecretGroups();
                    pathGroup = $"{SecreatGroupsPath}\\{nameGroup}";
                    break;
                case "ug":
                    groups = await userChats.FindUserGroups();
                    break;
                case "pg":
                    groups = userChats.FindPublicGroups();
                    needAddUser = true;
                    break;
            }
            if (groups != null/* || groups.Count != 0 */)
            {
                foreach (var group in groups)
                {
                    if (group == nameGroup)
                    {
                        if (needCreatePP)
                        {
                            return await CreateChat(nameGroup, typeGroup);
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
            return await fileMaster.UpdateFile<string>(path, invitations =>
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
            await fileMaster.UpdateFile<string>(path, allData =>
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
            await SendMessage($"If you want to join a group write: join{Environment.NewLine}" +
                "if you want to look at the invitations, write: look");
            while (true)
            {
                await AnswerClient();
                var message = user.communication.data.ToString();
                switch (message)
                {
                    case "join":
                        return await JoinToGroup();
                    case "look":
                        await SendGroups(userChats.Invitations, "Invitation:");
                        break;
                    default:
                        await SendMessage("Bed input, write else");
                        break;
                }
            }
        }
        private async Task<GroupInformation> JoinToGroup()
        {
            await SendMessage("Write the name of the group");
            while (true)
            {
                await AnswerClient();
                var groupName = user.communication.data.ToString();
                foreach (var invitation in userChats.Invitations)
                {
                    var normalInvitation = invitation.Remove(0, 8);
                    if (groupName == normalInvitation)
                    {
                        var groupInformation = await EnterTheGroup(invitation, groupName);
                        await SendMessage($"You have joined to the group{Environment.NewLine}" +
                            "If you want to open chats, write: 'open'");
                        await AnswerClient();
                        if (user.communication.data.ToString() == "open")
                        {
                            await SendMessage("You enter to the group");
                            return groupInformation;
                        }
                        else
                        {
                            await SendMessage("Ok, bye");
                            return new GroupInformation { CanOpenChat = false };
                        }
                    }
                }
                await SendMessage("Don`t have this invitation");
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
            await fileMaster.UpdateFile(path, fileMaster.AddData(information));
        }
        private async Task DeleteInvitation(string path, string invitation)
        {
            await fileMaster.UpdateFile<string>(path, users =>
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
                    await SendChats(false);
                    break;
                case "?/pp":
                    await SendGroups(userChats.AllElsePeople, "All people:");
                    break;
                case "?/ch":
                    await SendGroups(userChats.ChatsWithPeople, "Chat with people:");
                    break;
                case "?/gg":
                    await SendChats(true);
                    break;
                case "?/sg":
                    await SendGroups(userChats.SecretGroups, "Secret grout:");
                    break;
                case "?/ug":
                    await SendGroups(userChats.UserGroups, "User group:");
                    break;
                case "?/pg":
                    await SendGroups(userChats.PublicGroups, "Public group:");
                    break;
                case "?/ii":
                    await SendGroups(userChats.Invitations, "Invitation:");
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
        }
        private async Task HelpFindChat(string message)
        {
            var mode = message.Substring(2, 2);
            var beginningName = message.Remove(0, 4);
            switch (mode)
            {
                case "cc":
                    await SendChatsWithHelper(new List<List<string>> { userChats.AllElsePeople, userChats.ChatsWithPeople, userChats.SecretGroups,
                    userChats.UserGroups, userChats.PublicGroups, userChats.Invitations},
                    new string[] { "All people:", "Chats with people:", "Secret groups:", "User groups:", "Public groups:", "Invitations:" },
                    beginningName);
                    break;
                case "pp":
                    await SendChatsWithHelper(new List<List<string>> { userChats.AllElsePeople}, new string[] { "All people:" }, beginningName);
                    break;
                case "ch":
                    await SendChatsWithHelper(new List<List<string>> { userChats.ChatsWithPeople }, new string[] { "Chats with people:" }, beginningName);
                    break;
                case "gg":
                    await SendChatsWithHelper(new List<List<string>> { userChats.SecretGroups, userChats.UserGroups, userChats.PublicGroups, userChats.Invitations},
                    new string[] { "Secret groups:", "User groups:", "Public groups:", "Invitations:" }, beginningName);
                    break;
                case "sg":
                    await SendChatsWithHelper(new List<List<string>> { userChats.SecretGroups }, new string[] { "Secret groups:" }, beginningName);
                    break;
                case "ug":
                    await SendChatsWithHelper(new List<List<string>> { userChats.UserGroups }, new string[] { "User groups:" }, beginningName);
                    break;
                case "pg":
                    await SendChatsWithHelper(new List<List<string>> { userChats.PublicGroups }, new string[] { "Public groups:" }, beginningName);
                    break;
                case "ii":
                    await SendChatsWithHelper(new List<List<string>> { userChats.Invitations }, new string[] { "Invitations:" }, beginningName);
                    break;
            }
        }
        private async Task SendChatsWithHelper(List<List<string>> chats, string[] firstMessages, string beginningName)
        {
            var i = 0;
            foreach (var chat in chats)
            {
                await SendGroups((chat ?? new List<string>()).Where(x => x.Substring(0, beginningName.Length) == beginningName), firstMessages[i]);
                i++;
            }
        }
        private async Task SendChats(bool onlyGroup)
        {
            if (!onlyGroup)
            {
                await SendGroups(userChats.ChatsWithPeople, "Chats with people:");
                await SendGroups(userChats.AllElsePeople, "All people:");
            }
            await SendGroups(userChats.SecretGroups, "Secret groups:");
            await SendGroups(userChats.UserGroups, "User groups:");
            await SendGroups(userChats.PublicGroups, "Public groups:");
            await SendGroups(userChats.Invitations, "Invitations:");
        }
        private async Task SendGroups(IEnumerable<string> groups, string firstMassage)
        {
            await SendMessage(firstMassage);
            await AnswerClient();
            if (groups == null || groups.Count() == 0)
            {
                await SendMessage("0");
                await AnswerClient();
                return;
            }
            await SendMessage(groups.Count().ToString());
            await AnswerClient();
            foreach (var group in groups)
            {
                await SendMessage(group);
                await AnswerClient();
            }
        }
        private async Task SendMessage(string message)
        {
            await user.communication.SendMessage(message);
        }
        private async Task AnswerClient()
        {
            await user.communication.AnswerClient();
        }

    }
}
