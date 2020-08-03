using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class GroupsLeaver
    {
        public GroupsLeaver(string userNick, string nameGroup, string pathGroup, string typeGroup, string usersPath, FileMaster fileMaster)
        {
            this.NameGroup = nameGroup;
            this.UserNick = userNick;
            this.PathGroup = pathGroup;
            this.TypeGroup = typeGroup;
            this.UsersPath = usersPath;
            this.fileMaster = fileMaster;
            needChat = new PersonChat();
        }
        private string NameGroup { get; }
        private string UserNick { get; }
        private string PathGroup { get; }
        private string TypeGroup { get; }
        private string UsersPath { get; }
        private FileMaster fileMaster;
        private PersonChat needChat;
        public async Task<bool> Leave()
        {
            var pathElements = await Task.Run(() => FindPathsElement());
            if (!await DeleteData(pathElements))
            {
                await AddData(pathElements);
            }
            return true;
        }
        private string[] FindPathsElement()
        {
            if (TypeGroup == "pg" || TypeGroup == "ug")
            {
                return new string[] { "userGroups.json", "leavedUserGroups.json" };
            }
            else if (TypeGroup == "sg")
            {
                return new string[] { "secretGroups.json", "leavedSecretGroups.json" };
            }
            else if (TypeGroup == "pp" || TypeGroup == "ch")
            {
                return new string[] { "peopleChatsBeen.json", "leavedPeopleChatsBeen.json" };
            }
            return new string[0];
        }
        private async Task<bool> DeleteData(string[] pathsElement)
        {
            var needDeleteGroup = false;
            await fileMaster.UpdateFile<string>(Path.Combine(PathGroup, "users.json"), users =>
            {
                users.Remove(UserNick);
                if (users.Count == 0)
                {
                    needDeleteGroup = true;
                    return (users, false);
                }
                return (users, true);
            });
            if (needDeleteGroup)
            {
                await DeleteGroup();
            }

            if (pathsElement[0] == "peopleChatsBeen.json")
            {
                await DeletePeopleChatsBeen(pathsElement[0]);
            }
            else
            {
                await fileMaster.UpdateFile<string>(Path.Combine(UsersPath, UserNick, pathsElement[0]), nameChats =>
                {
                    return ((nameChats ?? new List<string>())
                    .Where(group => group != NameGroup)
                    .ToList(), true);
                });
            }
            return needDeleteGroup;
        }
        private async Task DeletePeopleChatsBeen(string pathsElement)
        {
            await fileMaster.UpdateFile<PersonChat>(Path.Combine(UsersPath, UserNick, pathsElement), nameChats =>
            {
                if (nameChats != null)
                {
                    foreach (var nameChat in nameChats)
                    {
                        if (nameChat.NameChat == NameGroup)
                        {
                            needChat = nameChat;
                            break;
                        }
                    }
                    nameChats.Remove(needChat);
                    return (nameChats, true);
                }
                else
                {
                    return (nameChats, false);
                }
            });
        }
        private async Task AddData(string[] pathsElement)
        {
            await fileMaster.UpdateFile(Path.Combine(PathGroup, "leavedPeople.json"), fileMaster.AddData(UserNick));
            if (pathsElement[0] == "peopleChatsBeen.json")
            {
                await AddPeopleChatsBeen(pathsElement[1]);
            }
            else
            {
                await fileMaster.UpdateFile(Path.Combine(UsersPath, UserNick, pathsElement[1]), fileMaster.AddData(NameGroup));
            }
        }
        private async Task AddPeopleChatsBeen(string pathsElement)
        {
            await fileMaster.UpdateFile(Path.Combine(UsersPath, UserNick, pathsElement), fileMaster.AddData(needChat));
        }
        private async Task DeleteGroup()
        {
            GroupDeleter groupDeleter = new GroupDeleter(NameGroup, PathGroup, TypeGroup, UsersPath, fileMaster);
            await groupDeleter.Run();
        }
    }
}
