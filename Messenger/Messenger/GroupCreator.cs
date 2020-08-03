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
    class GroupCreator
    {
        public GroupCreator(User user, Messenger messenger)
        {
            this.user = user;
            this.messenger = messenger;
        }
        private Messenger messenger;
        private User user;
        private string AskForClien { get { return $"Who do you want to invite to your group?{Environment.NewLine}" +
                $"If you want to check people, write ?/yes{Environment.NewLine}" +
                $"If you don`t want to add people, write ?/no{Environment.NewLine}";}}
        
        private async Task<string> SelectTypeGroup()
        {
            await SendMessage($"What type of group do you want?{Environment.NewLine}" +
                "If secret, write 'sg', if public, write 'pg'");
            while (true)
            {
                await AnswerClient();
                var typeGroup = user.communication.data.ToString();
                if (typeGroup == "sg" || typeGroup == "pg")
                {
                    return typeGroup;
                }
                await SendMessage($"Bed input{Environment.NewLine}" +
                    "If secret, write 'sg', if public, write 'pg'");
            }
        }
        private async Task<string> SelectGroupName(string typeGroup)
        {
            await SendMessage("Enter a group name");
            while (true)
            {
                await AnswerClient();
                var groupName = user.communication.data.ToString();
                var goodInput = CharacterCheckers.CheckInput(groupName);
                if (!goodInput)
                {
                    await SendMessage("The group name can only contain lowercase letters and numbers");
                    continue;
                }
                else if (!CheckGroups(groupName, typeGroup))
                {
                    await SendMessage("A group with this name has already been created, enter new plese");
                    continue;
                }
                return groupName;
            }
        }
        private bool CheckGroups(string nameGroup, string typeGroup)
        {
            switch (typeGroup)
            {
                case "pg":
                    return !(FileMaster.GetDirectories(messenger.Server.PublicGroupPath) ?? new string[0])
                        .Select(path => FileMaster.GetFileName(path))
                        .Contains(nameGroup);
                default: //(sg)
                    return !(FileMaster.GetDirectories(messenger.Server.SecretGroupPath) ?? new string[0])
                        .Select(path => FileMaster.GetFileName(path))
                        .Contains(nameGroup);
            }
        }
        public async Task<GroupInformation> Run()
        {
            var typeGroup = await SelectTypeGroup();
            var nameGroup = await SelectGroupName(typeGroup);
            await SendMessage(AskForClien);
            bool canCreate = false;
            var people = await FindChatsPeople();
            var invitedPeople = new List<string>();
            while (true)
            {
                await AnswerClient();
                if (user.communication.data.ToString() == "?/yes")
                {
                    people = await FindChatsPeople();
                    await SendGroups(people, "People:");
                }
                else if (user.communication.data.ToString() == "?/no")
                {
                    if (canCreate)
                    {
                        return await CreateNewGroup(nameGroup, typeGroup, invitedPeople);
                    }
                    else
                    {
                        await SendMessage("You can`t create group when you are alone in it, write other name group");
                    }
                }
                else
                {
                    var nick = user.communication.data.ToString();
                    if (CheckPeople(people, nick))
                    {
                        if (CheckInInvitedList(invitedPeople, nick))
                        {
                            invitedPeople.Add(nick);
                            canCreate = true;
                            await SendMessage($"Ok, person was invited{Environment.NewLine}" +
                                $"{AskForClien}");
                        }
                        else
                        {
                            await SendMessage($"You invited this person{Environment.NewLine}{Environment.NewLine}" +
                                $"{AskForClien}");
                        }
                    }
                    else
                    {
                        await SendMessage($"Don`t have this nickname{Environment.NewLine}{Environment.NewLine}" +
                            $"{AskForClien}");
                    }
                }
            }
        }
        private bool CheckInInvitedList(List<string> invitedPeople, string nick)
        {
            return !invitedPeople.Contains(nick);
        }
        private async Task<GroupInformation> CreateNewGroup(string nameGroup, string typeGroup, List<string> invitedPeople)
        {
            var pathGroup = await AddGroup(nameGroup, typeGroup, invitedPeople);
            await InvitePeople(invitedPeople, nameGroup, typeGroup);
            await SendMessage($"You create group, thanks.{Environment.NewLine}" +
                "If you want to open it, write ok, else - press else");
            await AnswerClient();
            if (user.communication.data.ToString() == "ok")
            {
                return new GroupInformation (true, typeGroup, nameGroup, pathGroup );
            }
            return new GroupInformation(false, typeGroup, nameGroup, pathGroup);
        }
        private bool CheckPeople(IEnumerable<string> people, string nick)
        {
            return people.Contains(nick);
        }
        private async Task<string> AddGroup(string nameGroup, string typeGroup, List<string> invitedPeople)
        {
            string pathGroup;
            switch (typeGroup)
            {
                case "pg":
                    pathGroup = Path.Combine(messenger.Server.PublicGroupPath, nameGroup);
                    break;
                default: //(sg)
                    pathGroup = Path.Combine(messenger.Server.SecretGroupPath, nameGroup);
                    break;
            }
            FileMaster.CreateDirectory(pathGroup);
            await FileMaster.UpdateFile(Path.Combine(pathGroup, "users.json"), FileMaster.AddData(user.Nickname));
            await FileMaster.UpdateFile(Path.Combine(pathGroup, "invitation.json"), FileMaster.AddSomeData(invitedPeople));
            return pathGroup;
        }
        private async Task InvitePeople(IEnumerable<string> invitedPeople, string nameGroup, string typeGroup)
        {
            switch (typeGroup)
            {
                case "pg":
                    await ReadWriteInvitationOrGroup(Path.Combine(messenger.Server.UsersPath, user.Nickname, "userGroups.json"), nameGroup, "my group");
                    break;
                default: //(sg)
                    await ReadWriteInvitationOrGroup(Path.Combine(messenger.Server.UsersPath, user.Nickname, "secretGroups.json"), nameGroup, "my group");
                    break;
            }
            foreach (var invitedPerson in invitedPeople)
            {
                await ReadWriteInvitationOrGroup(Path.Combine(messenger.Server.UsersPath, invitedPerson, "invitation.json"), nameGroup, typeGroup); //////////////////// check if somebode delete your nick?
            }
        }
        private async Task ReadWriteInvitationOrGroup(string path, string nameGroup, string typeGroup)
        {
            string data;
            if (typeGroup == "pg")
            {
                data = $"public: {nameGroup}";
            }
            else if (typeGroup == "sg")
            {
                data = $"secret: {nameGroup}";
            }
            else
            {
                data = nameGroup;
            }
            await FileMaster.UpdateFile(path, FileMaster.AddData(data));
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
        private async Task<List<string>> FindChatsPeople()
        {
            return ((await FileMaster.ReadAndDeserialize<UserNicknameAndPasswordAndIPs>(Path.Combine(messenger.Server.NicknamesAndPasswordsPath, "users.json")))
                ?? new List<UserNicknameAndPasswordAndIPs>())
                .Select(user => user.Nickname)
                .Where(nickname => nickname != user.Nickname)
                .ToList();
        }







        private async Task AnswerClient()
        {
            await user.communication.AnswerClient();
        }
        private async Task SendMessage(string message)
        {
            await user.communication.SendMessage(message);
        }
    }
}
