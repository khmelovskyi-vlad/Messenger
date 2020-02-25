using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class GroupDeleter
    {
        public GroupDeleter(string groupName, string pathChat, string typeChat, FileMaster fileMaster)
        {
            this.NameChat = groupName;
            this.pathChat = pathChat;
            this.typeChat = typeChat;
            this.fileMaster = fileMaster;
        }
        private string pathChat;
        private string typeChat;
        private string NameChat;
        private FileMaster fileMaster;
        public async Task Run()
        {
            var pathElement = "";
            var invitationName = "";
            if (typeChat == "pg" || typeChat == "ug")
            {
                invitationName = $"public: {NameChat}";
                pathElement = "leavedUserGroups";
            }
            else if (typeChat == "sg")
            {
                invitationName = $"secret: {NameChat}";
                pathElement = "leavedSecretGroups";
            }
            else if (typeChat == "pp" || typeChat == "ch") 
            {
                pathElement = "leavedPeopleChatsBeen";
            }


            var invitationPaths = await FindUserPath($"{pathChat}\\invitation.json", "\\invitation.json");
            if (invitationPaths != null)
            {
                await DeleteExtraData(invitationPaths, invitationName);
            }
            var leavedPaths = await FindUserPath($"{pathChat}\\leavedPeople.json", $"\\{pathElement}.json");
            if (leavedPaths != null)
            {
                if (typeChat == "pp" || typeChat == "ch")
                {
                    await DeleteLeavedPeople(leavedPaths);
                    return;
                }
                await DeleteExtraData(leavedPaths, NameChat);
            }
        }
        private async Task DeleteLeavedPeople(List<string> paths)
        {
            foreach (var path in paths)
            {
                await fileMaster.ReadWrite(path, nameChats =>
                {
                    var needChat = new PersonChat(new string[0], "");
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
                });
            }
        }
        private async Task DeleteExtraData(List<string> paths, string nameChat)
        {
            foreach (var path in paths)
            {
                await fileMaster.ReadWrite(path, users =>
                {
                    users.Remove(nameChat);
                    return (users, true);
                });
            }
        }
        private async Task<List<string>> FindUserPath(string path, string lastPartOfPath)
        {
            var leavedPaths = new List<string>();
            var users = await fileMaster.ReadAndDesToLString(path);
            if (users == null)
            {
                return null;
            }
            foreach (var user in users)
            {
                leavedPaths.Add($@"D:\temp\messenger\Users\{user}{lastPartOfPath}");
            }
            return leavedPaths;
        }
    }
}
