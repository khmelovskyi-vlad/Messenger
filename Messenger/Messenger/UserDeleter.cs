using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class UserDeleter
    {

        private const string PublicGroupsPath = @"D:\temp\messenger\publicGroup";
        private const string SecretGroupsPath = @"D:\temp\messenger\secretGroup";
        private const string PeopleChatsPath = @"D:\temp\messenger\peopleChats";
        private const string Users = @"D:\temp\messenger\Users";
        public UserDeleter(FileMaster fileMaster)
        {
            this.fileMaster = fileMaster;
        }
        private FileMaster fileMaster;
        private string user;
        public async Task Run(string nick, bool needChangeMessages)
        {
            user = nick;
            await DeleteData(PublicGroupsPath, $@"{Users}\{user}\userGroups.json", $@"{Users}\{user}\leavedUserGroups.json", "ug", needChangeMessages);
            await DeleteData(SecretGroupsPath, $@"{Users}\{user}\secretGroups.json", $@"{Users}\{user}\leavedSecretGroups.json", "sg", needChangeMessages);
            await DeleteData(PeopleChatsPath, $@"{Users}\{user}\peopleChatsBeen.json", $@"{Users}\{user}\leavedPeopleChatsBeen.json", "pp", needChangeMessages);
            await DeleteInvitations();
        }
        private async Task DeleteData(string pathTypeGroup, string pathUseGroups, string pathLeavedGroups, string typeGroups, bool needChangeMessages)
        {
            var groups = await fileMaster.ReadAndDesToLString(pathUseGroups);
            var leavedGroups = await fileMaster.ReadAndDesToLString(pathLeavedGroups);
            if (needChangeMessages)
            {
                await ChangeMessages(pathTypeGroup, groups, leavedGroups);
            }
            await DeleteNickInGroups(groups, pathTypeGroup, "users.json", typeGroups);
            await DeleteNickInGroups(leavedGroups, pathTypeGroup, "leavedPeople.json", typeGroups);
            //fileMaster.DeleterFolder($"{Users}\\{user}");
        }
        private async Task DeleteNickInGroups(List<string> groupNames, string firstPartOfThePast, string lastPartOfThePast, string typeGroups)
        {
            if (groupNames != null)
            {
                foreach (var groupName in groupNames)
                {
                    var path = $"{firstPartOfThePast}\\{groupName}\\{lastPartOfThePast}";
                    var needDeleteGroup = false;
                    await fileMaster.ReadWrite(path, (users) =>
                    {
                        users.Remove(user);
                        if (lastPartOfThePast == "users.json")
                        {
                            if (users.Count == 0)
                            {
                                needDeleteGroup = true;
                            }
                        }
                        return (users, true);
                    });
                    if (needDeleteGroup)
                    {
                        GroupDeleter groupDeleter = new GroupDeleter(groupName, $"{firstPartOfThePast}\\{groupName}", typeGroups, fileMaster);
                        await groupDeleter.Run();
                    }
                }
            }
        }

        private async Task ChangeMessages(string path, List<string> firstGroups, List<string> secondGroups)
        {
            await ChangeMessages(path, firstGroups);
            await ChangeMessages(path, secondGroups);
            //if (firstGroups != null)
            //{
            //    var firstGroupsPaths = firstGroups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
            //    foreach (var firstGroupPath in firstGroupsPaths)
            //    {
            //        await ReadWriteData(firstGroupPath);
            //    }
            //}
            //if (secondGroups != null)
            //{
            //    var SecondGroupsPaths = secondGroups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
            //    foreach (var SecondGroupPath in SecondGroupsPaths)
            //    {
            //        await ReadWriteData(SecondGroupPath);
            //    }
            //}
        }
        private async Task ChangeMessages(string path, List<string> groups)
        {
            if (groups != null)
            {
                var groupsPaths = groups.Select(nameGroup => $@"{path}\{nameGroup}\data.json");
                foreach (var groupPaths in groupsPaths)
                {
                    await ReadWriteData(groupPaths);
                }
            }
        }
        private async Task ReadWriteData(string path)
        {
            //var messages = await fileMaster.ReadAndDesToLString(path);
            await fileMaster.ReadWrite(path, (messages) =>
            {
                var newMessages = new List<string>();
                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        if (user == message.Remove(user.Length))
                        {
                            newMessages.Add($"{user} was banned");
                            continue;
                        }
                        newMessages.Add(message);
                    }
                }
                return (newMessages, true);
            });
        }
        private async Task DeleteInvitations()
        {
            var invitations = await fileMaster.ReadAndDesToLString($@"{Users}\{user}\invitation.json");
            if (invitations != null)
            {
                foreach (var invitation in invitations)
                {
                    string path;
                    switch (invitation[0])
                    {
                        case 'p':
                            path = $@"{PublicGroupsPath}\{invitation.Remove(0, 8)}\invitation.json";
                            break;
                        case 's':
                            path = $@"{SecretGroupsPath}\{invitation.Remove(0, 8)}\invitation.json";
                            break;
                        default:
                            path = "";
                            break;
                    }
                    await fileMaster.ReadWrite(path, (users) =>
                    {
                        users.Remove(user);
                        return (users, true);
                    });
                }
            }
        }

    }
}
