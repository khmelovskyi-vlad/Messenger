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
    class CreatorGroups
    {
        public CreatorGroups(User user)
        {
            this.user = user;
        }
        private string PublicGroupsPath { get { return @"D:\temp\messenger\publicGroup"; } }
        private string SecreatGroupsPath { get { return @"D:\temp\messenger\secretGroup"; } }
        private User user;
        const string askForClien = "Who do you want to invite to your group?\n\r" +
                "If you want to check people, write ?/yes\n\r" +
                "If you don`t want to add people, write ?/no\n\r";

        FileMaster fileMaster = new FileMaster();
        private string SelectTypeGroup()
        {
            SendMessage("What type of group do you want?\n\r" +
                "If secret, write 'sg', if public, write 'pg'");
            while (true)
            {
                AnswerClient();
                var typeGroup = user.communication.data.ToString();
                if (typeGroup == "sg" || typeGroup == "pg")
                {
                    return typeGroup;
                }
                SendMessage("Bed input\n\r" +
                    "If secret, write 'sg', if public, write 'pg'");
            }
        }
        private string SelectGroupName(string typeGroup)
        {
            SendMessage("Enter a group name");
            while (true)
            {
                AnswerClient();
                var groupName = user.communication.data.ToString();
                var findSymdol = false;
                foreach (var symbol in groupName)
                {
                    if (symbol == '\\' || symbol == '/' || symbol == ':' || symbol == '*' || symbol == '?'
                        || symbol == '"' || symbol == '<' || symbol == '>' || symbol == '|')
                    {
                        findSymdol = true;
                        var invertedComma = '"';
                        SendMessage($"nickname cannot contain characters such as:\n\r' ', '\\', '/', ':', '*', '?', '{invertedComma}', '<', '>', '|'");
                        break;
                    }
                }
                if (findSymdol)
                {
                    continue;
                }
                else if (!CheckGroups(groupName, typeGroup))
                {
                    SendMessage("A group with this name has already been created, enter new plese");
                    continue;
                }
                return groupName;
            }
        }
        private bool CheckGroups(string nameGroup, string typeGroup)
        {
            string[] groups;
            if (typeGroup == "pg")
            {
                groups = fileMaster.GetDirectories(@"D:\temp\messenger\publicGroup");
            }
            else
            {
                groups = fileMaster.GetDirectories(@"D:\temp\messenger\secretGroup");
            }
            foreach (var group in groups)
            {
                var groupWithoutPath = fileMaster.GetFileName(group);
                if (nameGroup == groupWithoutPath)
                {
                    return false;
                }
            }
            return true;
        }
        public async Task<string[]> Run()
        {
            var typeGroup = SelectTypeGroup();
            var nameGroup = SelectGroupName(typeGroup);
            SendMessage(askForClien);
            bool canCreate = false;
            var people = await FindChatsPeople();
            var invitedPeople = new List<string>();
            while (true)
            {
                AnswerClient();
                if (user.communication.data.ToString() == "?/yes")
                {
                    people = await FindChatsPeople();
                    SendGroups(people, "People:");
                }
                else if (user.communication.data.ToString() == "?/no")
                {
                    if (canCreate)
                    {
                        return await CreateNewGroup(nameGroup, typeGroup, invitedPeople);
                    }
                    else
                    {
                        SendMessage("You can`t create group when you are alone in it, write other name group");
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
                            SendMessage(askForClien);
                        }
                        else
                        {
                            SendMessage($"You invited this person\n\r\n\r" +
                                $"{askForClien}");
                        }
                    }
                    else
                    {
                        SendMessage($"Don`t have this nickname\n\r\n\r" +
                            $"{askForClien}");
                    }
                }
            }
        }
        private bool CheckInInvitedList(List<string> invitedPeople, string nick)
        {
            foreach (var invitedPerson in invitedPeople)
            {
                if (invitedPerson == nick)
                {
                    return false;
                }
            }
            return true;
        }
        private async Task<string[]> CreateNewGroup(string nameGroup, string typeGroup, List<string> invitedPeople)
        {
            var pathGroup = await AddGroup(nameGroup, typeGroup, invitedPeople);
            InvitePeople(invitedPeople, nameGroup, typeGroup);
            SendMessage("You create group, thanks.\n\r" +
                "If you want to open it, write ok, else - press else");
            AnswerClient();
            if (user.communication.data.ToString() == "ok")
            {
                return new string[] { typeGroup, nameGroup, pathGroup };
            }
            return new string[] { "", nameGroup, pathGroup };
        }
        private bool CheckPeople(IEnumerable<string> people, string nick)
        {
            foreach (var person in people)
            {
                if (person == nick)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task<string> AddGroup(string nameGroup, string typeGroup, List<string> invitedPeople)
        {
            StringBuilder directoryName = new StringBuilder();
            if (typeGroup == "pg")
            {
                directoryName.Append($"{PublicGroupsPath}\\{nameGroup}");
            }
            else if (typeGroup == "sg")
            {
                directoryName.Append($"{SecreatGroupsPath}\\{nameGroup}");
            }
            //var timeString = DateTime.Now.ToString();
            //StringBuilder normalTime = new StringBuilder();
            //foreach (var timeChar in timeString)
            //{
            //    if (timeChar == ':')
            //    {
            //        normalTime.Append('.');
            //        continue;
            //    }
            //    normalTime.Append(timeChar);
            //}
            //Directory.CreateDirectory($"{directoryName}{normalTime}");
            var pathGroup = directoryName.ToString();
            fileMaster.CreateDirectory(pathGroup);
            AddUser($"{pathGroup}\\users.json");
            await WriteInvite($"{pathGroup}\\invitation.json", invitedPeople);
            return pathGroup;
        }
        private async Task WriteInvite(string path, List<string> invitedPeople)
        {
            using (var stream = File.Open(path, FileMode.CreateNew, FileAccess.Write))
            {
                var users = new List<string>();
                var invitedPeopleJson = JsonConvert.SerializeObject(invitedPeople);
                var buffer = Encoding.Default.GetBytes(invitedPeopleJson);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        private async void AddUser(string path)
        {
            using (var stream = File.Open(path, FileMode.Create, FileAccess.Write))
            {
                var users = new List<string>();
                users.Add(user.Nickname);
                var peopleChatsBeenJson = JsonConvert.SerializeObject(users);
                var buffer = Encoding.Default.GetBytes(peopleChatsBeenJson);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        private void InvitePeople(IEnumerable<string> invitedPeople, string nameGroup, string typeGroup)
        {
            if (typeGroup == "pg")
            {
                ReadWriteInvitationOrGroup($@"D:\temp\messenger\Users\{user.Nickname}\userGroups.json", nameGroup, "my group");
            }
            else if (typeGroup == "sg")
            {
                ReadWriteInvitationOrGroup($@"D:\temp\messenger\Users\{user.Nickname}\secretGroups.json", nameGroup, "my group");
            }
            foreach (var invitedPerson in invitedPeople)
            {
                ReadWriteInvitationOrGroup($@"D:\temp\messenger\Users\{invitedPerson}\invitation.json", nameGroup, typeGroup); //////////////////// check if somebode delete your nick?
            }
        }
        private async void ReadWriteInvitationOrGroup(string path, string nameGroup, string typeGroup)
        {
            await fileMaster.ReadWrite(path, usersInvitation =>
            {
                if (usersInvitation == null)
                {
                    usersInvitation = new List<string>();
                }
                if (typeGroup == "pg")
                {
                    usersInvitation.Add($"public: {nameGroup}");
                }
                else if (typeGroup == "sg")
                {
                    usersInvitation.Add($"secret: {nameGroup}");
                }
                else
                {
                    usersInvitation.Add(nameGroup);
                }
                return (usersInvitation, true);
            });
        }
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
        private async Task<List<string>> FindChatsPeople()
        {
            var allPeopleJson = await fileMaster.ReadAndDesToLUserInf($@"D:\temp\messenger\nicknamesAndPasswords\users.json");
            var allPeopleWithoutPasswordJsons = new List<string>();
            if (allPeopleJson != null)
            {
                foreach (var personJson in allPeopleJson)
                {
                    if (personJson.Nickname != user.Nickname)
                    {
                        allPeopleWithoutPasswordJsons.Add(personJson.Nickname);
                    }
                }
            }
            return allPeopleWithoutPasswordJsons;
        }






        
        private void AnswerClient()
        {
            user.communication.AnswerClient();
        }
        private void SendMessage(string message)
        {
            user.communication.SendMessage(message);
        }
    }
}
