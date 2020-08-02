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
            var invitationName = "";
            var pathElement = "";
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

            if (invitationName != "")
            {
                var invitationPaths = await FindUserPath($"{pathChat}\\invitation.json", "\\invitation.json");
                if (invitationPaths != null || invitationPaths.Count() != 0)
                {
                    await DeleteExtraData(invitationPaths, invitationName);
                }
            }
            var leavedPaths = await FindUserPath($"{pathChat}\\leavedPeople.json", $"\\{pathElement}.json");
            if (leavedPaths != null || leavedPaths.Count() != 0)
            {
                if (typeChat == "pp" || typeChat == "ch")
                {
                    await DeleteLeavedPeople(leavedPaths);
                }
                else
                {
                    await DeleteExtraData(leavedPaths, NameChat);
                }
            }
            fileMaster.DeleterFolder(pathChat);
        }
        private async Task DeleteLeavedPeople(List<string> paths)
        {
            foreach (var path in paths)
            {
                await fileMaster.UpdateFile<PersonChat>(path, nameChats =>
                {
                    return ((nameChats ?? new List<PersonChat>())
                    .Where(chat => chat.NameChat != NameChat)
                    .ToList(), true);
                });
            }
        }
        private async Task DeleteExtraData(List<string> paths, string nameChat)
        {
            foreach (var path in paths)
            {
                await fileMaster.UpdateFile<string>(path, users =>
                {
                    users.Remove(nameChat);
                    return (users, true);
                });
            }
        }
        private async Task<List<string>> FindUserPath(string path, string lastPartOfPath)
        {
            return ((await fileMaster.ReadAndDeserialize<string>(path))
                ?? new List<string>())
                .Select(user => $@"D:\temp\messenger\Users\{user}{lastPartOfPath}")
                .ToList();
        }
    }
}
