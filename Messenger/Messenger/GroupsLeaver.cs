using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class GroupsLeaver
    {
        public GroupsLeaver(string userNick, string pathChat, string typeChat, string nameChat, FileMaster fileMaster)
        {
            this.NameChat = nameChat;
            this.userNick = userNick;
            this.pathChat = pathChat;
            this.typeChat = typeChat;
            this.fileMaster = fileMaster;
            needChat = new PersonChat();
        }
        private string NameChat { get; }
        private string userNick;
        private string pathChat;
        private string typeChat;
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
            if (typeChat == "pg" || typeChat == "ug")
            {
                return new string[] { "userGroups", "leavedUserGroups"};
            }
            else if (typeChat == "sg")
            {
                return new string[] { "secretGroups", "leavedSecretGroups" };
            }
            else if (typeChat == "pp" || typeChat == "ch")
            {
                return new string[] { "peopleChatsBeen", "leavedPeopleChatsBeen" };
            }
            return new string[0];
        }
        private async Task<bool> DeleteData(string[] pathsElement)
        {
            var needDeleteGroup = false;
            await fileMaster.UpdateFile<string>($"{pathChat}\\users.json", users =>
            {
                users.Remove(userNick);
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

            if (pathsElement[0] == "peopleChatsBeen")
            {
                await DeletePeopleChatsBeen(pathsElement[0]);
            }
            else
            {
                await fileMaster.UpdateFile<string>($@"D:\temp\messenger\Users\{userNick}\{pathsElement[0]}.json", nameChats =>
                {
                    return ((nameChats ?? new List<string>())
                    .Where(chat => chat != NameChat)
                    .ToList(), true);
                });
            }
            return needDeleteGroup;
        }
        private async Task DeletePeopleChatsBeen(string pathsElement)
        {
            await fileMaster.UpdateFile<PersonChat>($@"D:\temp\messenger\Users\{userNick}\{pathsElement}.json", nameChats =>
            {
                if (nameChats != null)
                {
                    foreach (var nameChat in nameChats)
                    {
                        if (nameChat.NameChat == NameChat)
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
            await fileMaster.UpdateFile($"{pathChat}\\leavedPeople.json", fileMaster.AddData(userNick));
            if (pathsElement[0] == "peopleChatsBeen")
            {
                await AddPeopleChatsBeen("leavedPeopleChatsBeen");
            }
            else
            {
                await fileMaster.UpdateFile($@"D:\temp\messenger\Users\{userNick}\{pathsElement[1]}.json", fileMaster.AddData(NameChat));
            }
        }
        private async Task AddPeopleChatsBeen(string pathsElement)
        {
            await fileMaster.UpdateFile($@"D:\temp\messenger\Users\{userNick}\{pathsElement}.json", fileMaster.AddData(needChat));
        }
        private async Task DeleteGroup()
        {
            GroupDeleter groupDeleter = new GroupDeleter(NameChat, pathChat, typeChat, fileMaster);
            await groupDeleter.Run();
        }
    }
}
