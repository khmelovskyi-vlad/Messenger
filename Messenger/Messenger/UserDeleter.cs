using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class UserDeleter
    {
        public UserDeleter(Messenger messenger)
        {
            this.messenger = messenger;
        }
        private Messenger messenger;
        private string userNickname;
        public async Task Run(string nick, bool needChangeMessages)
        {
            userNickname = nick;
            await DeleteData(messenger.Server.PublicGroupPath,
                await FileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.UsersPath, userNickname, "userGroups.json")),
                await FileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.UsersPath, userNickname, "leavedUserGroups.json")),
                "ug",
                needChangeMessages);

            await DeleteData(messenger.Server.SecretGroupPath,
                await FileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.UsersPath, userNickname, "secretGroups.json")),
                await FileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.UsersPath, userNickname, "leavedSecretGroups.json")),
                "sg",
                needChangeMessages);
            await DeleteData(messenger.Server.PeopleChatsPath,
                ((await FileMaster.ReadAndDeserialize<PersonChat>(Path.Combine(messenger.Server.UsersPath, userNickname, "peopleChatsBeen.json")))
                ?? new List<PersonChat>()).Select(chat => chat.NameChat).ToList(),
                ((await FileMaster.ReadAndDeserialize<PersonChat>(Path.Combine(messenger.Server.UsersPath, userNickname, "leavedPeopleChatsBeen.json")))
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
                    var path = Path.Combine(firstPartOfThePast, groupName, lastPartOfThePast);
                    var needDeleteGroup = false;
                    await FileMaster.UpdateFile<string>(path, (users) =>
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
                        GroupDeleter groupDeleter = new GroupDeleter(groupName, 
                            Path.Combine(firstPartOfThePast, groupName), 
                            typeGroups, 
                            messenger.Server.UsersPath);
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
                var groupsPaths = groups.Select(nameGroup => Path.Combine(path, nameGroup, "data.json"));
                foreach (var groupPaths in groupsPaths)
                {
                    await ReadWriteData(groupPaths);
                }
            }
        }
        private async Task ReadWriteData(string path)
        {
            await FileMaster.UpdateFile<string>(path, (messages) =>
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
            var invitations = await FileMaster.ReadAndDeserialize<string>(Path.Combine(messenger.Server.UsersPath, userNickname, "invitation.json"));
            if (invitations != null)
            {
                foreach (var invitation in invitations)
                {
                    string path;
                    switch (invitation[0])
                    {
                        case 'p':
                            path = Path.Combine(messenger.Server.PublicGroupPath, invitation.Remove(0, 8), "invitation.json");
                            break;
                        case 's':
                            path = Path.Combine(messenger.Server.SecretGroupPath, invitation.Remove(0, 8), "invitation.json");
                            break;
                        default:
                            path = "";
                            break;
                    }
                    await FileMaster.UpdateFile<string>(path, (users) =>
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
