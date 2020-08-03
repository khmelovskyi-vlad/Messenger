using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class GroupDeleter
    {
        public GroupDeleter(string nameGroup, string pathGroup, string typeGroup, string usersPath, FileMaster fileMaster)
        {
            this.NameChat = nameGroup;
            this.PathChat = pathGroup;
            this.TypeChat = typeGroup;
            this.UsersPath = usersPath;
            this.fileMaster = fileMaster;
        }
        private string NameChat { get; }
        private string PathChat { get; }
        private string TypeChat { get; }
        private string UsersPath { get; }
        private FileMaster fileMaster;
        public async Task Run()
        {
            var invitationName = "";
            var pathElement = "";
            if (TypeChat == "pg" || TypeChat == "ug")
            {
                invitationName = $"public: {NameChat}";
                pathElement = "leavedUserGroups";
            }
            else if (TypeChat == "sg")
            {
                invitationName = $"secret: {NameChat}";
                pathElement = "leavedSecretGroups";
            }
            else if (TypeChat == "pp" || TypeChat == "ch") 
            {
                pathElement = "leavedPeopleChatsBeen";
            }

            if (invitationName != "")
            {
                var invitationPaths = await FindUserPath(Path.Combine(PathChat, "invitation.json"), "invitation.json");
                if (invitationPaths != null || invitationPaths.Count() != 0)
                {
                    await DeleteExtraData(invitationPaths, invitationName);
                }
            }
            var leavedPaths = await FindUserPath(Path.Combine(PathChat, "leavedPeople.json"), $"{pathElement}.json");
            if (leavedPaths != null || leavedPaths.Count() != 0)
            {
                if (TypeChat == "pp" || TypeChat == "ch")
                {
                    await DeleteLeavedPeople(leavedPaths);
                }
                else
                {
                    await DeleteExtraData(leavedPaths, NameChat);
                }
            }
            fileMaster.DeleterFolder(PathChat);
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
                .Select(user => Path.Combine(UsersPath, user, lastPartOfPath))
                .ToList();
        }
    }
}
