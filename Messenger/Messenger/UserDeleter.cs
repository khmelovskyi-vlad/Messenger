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
        private const string UsersPath = @"D:\temp\messenger\Users";
        public UserDeleter(FileMaster fileMaster)
        {
            this.fileMaster = fileMaster;
        }
        private FileMaster fileMaster;
        private string userNickname;
        public async Task Run(string nick, bool needChangeMessages)
        {
            userNickname = nick;
            await DeleteData(PublicGroupsPath,
                await fileMaster.ReadAndDeserialize<string>($@"{UsersPath}\{userNickname}\userGroups.json"),
                await fileMaster.ReadAndDeserialize<string>($@"{UsersPath}\{userNickname}\leavedUserGroups.json"),
                "ug",
                needChangeMessages);

            await DeleteData(SecretGroupsPath,
                await fileMaster.ReadAndDeserialize<string>($@"{UsersPath}\{userNickname}\secretGroups.json"),
                await fileMaster.ReadAndDeserialize<string>($@"{UsersPath}\{userNickname}\leavedSecretGroups.json"),
                "sg",
                needChangeMessages);
            await DeleteData(PeopleChatsPath,
                ((await fileMaster.ReadAndDeserialize<PersonChat>($@"{UsersPath}\{userNickname}\peopleChatsBeen.json"))
                ?? new List<PersonChat>()).Select(chat => chat.NameChat).ToList(),
                ((await fileMaster.ReadAndDeserialize<PersonChat>($@"{UsersPath}\{userNickname}\leavedPeopleChatsBeen.json"))
                ?? new List<PersonChat>()).Select(chat => chat.NameChat).ToList(),
                "pp",
                needChangeMessages);
            await DeleteInvitations();
            //fileMaster.DeleterFolder($"{Users}\\{user}");
        }
        private async Task DeleteData(string pathTypeGroup, List<string> groups, List<string> leavedGroups, string typeGroups, bool needChangeMessages)
        {
            if (needChangeMessages)
            {
                await ChangeMessages(pathTypeGroup, groups, leavedGroups);
            }
            await DeleteNickInGroups(leavedGroups, pathTypeGroup, "leavedPeople.json", typeGroups);
            await DeleteNickInGroups(groups, pathTypeGroup, "users.json", typeGroups);
        }
        private async Task DeleteNickInGroups(List<string> groupNames, string firstPartOfThePast, string lastPartOfThePast, string typeGroups)
        {
            if (groupNames != null)
            {
                foreach (var groupName in groupNames)
                {
                    var path = $"{firstPartOfThePast}\\{groupName}\\{lastPartOfThePast}";
                    var needDeleteGroup = false;
                    await fileMaster.UpdateFile<string>(path, (users) =>
                    {
                        users.Remove(userNickname);
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
            await fileMaster.UpdateFile<string>(path, (messages) =>
            {
                if (messages != null)
                {
                    return (messages.
                    Select(message => userNickname == message.Remove(userNickname.Length) ? $"{userNickname} was banned" : message)
                    .ToList(), true);
                }
                return (messages, false);
            });
        }
        private async Task DeleteInvitations()
        {
            var invitations = await fileMaster.ReadAndDeserialize<string>($@"{UsersPath}\{userNickname}\invitation.json");
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
                    await fileMaster.UpdateFile<string>(path, (users) =>
                    {
                        if (users == null)
                        {
                            users = new List<string>();
                        }
                        users.Remove(userNickname);
                        return (users, true);
                    });
                }
            }
        }

    }
}
