using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class UserChats
    {
        public UserChats(FileMaster fileMaster, string nick, 
            string userFoldersPath, string publicGroupsPath, string secreatGroupsPath, string peopleChatsPath)
        {
            this.fileMaster = fileMaster;
            this.nick = nick;
            this.userFoldersPath = userFoldersPath;
            this.publicGroupsPath = publicGroupsPath;
            this.secreatGroupsPath = secreatGroupsPath;
            this.peopleChatsPath = peopleChatsPath;
        }
        private string nick;
        private string userFoldersPath;
        private string publicGroupsPath;
        private string secreatGroupsPath;
        private string peopleChatsPath;
        private FileMaster fileMaster;
        //public List<string> AllChats;
        //public List<string> AllGroups;
        public List<string> ChatsWithPeople;
        public List<string> AllPeople;
        public List<string> SecretGroups;
        public List<string> UserGroups;
        public List<string> PublicGroups;
        public List<string> Invitations;
        private async Task FindAllChats()
        {
            ChatsWithPeople = ((await fileMaster.ReadAndDesToPersonCh($@"{userFoldersPath}\{nick}\peopleChatsBeen.json"))
                ?? new List<PersonChat>())
                .Select(chat => chat.Nicknames[0] != nick ? chat.Nicknames[0] : chat.Nicknames[1])
                .ToList();
            AllPeople = ((await fileMaster.ReadAndDesToLUserInf($@"D:\temp\messenger\nicknamesAndPasswords\users.json"))
                ?? new List<UserNicknameAndPasswordAndIPs>())
                .Select(x => x.Nickname)
                .Where(x => x != nick)
                .Except(ChatsWithPeople)
                .ToList();
            SecretGroups = await fileMaster.ReadAndDesToLString($@"{userFoldersPath}\{nick}\secretGroups.json");
            UserGroups = await fileMaster.ReadAndDesToLString($@"{userFoldersPath}\{nick}\userGroups.json");
            PublicGroups = fileMaster.GetDirectories(@"D:\temp\messenger\publicGroup")
                .Select(path => fileMaster.GetFileName(path))
                .ToList();
            Invitations = await fileMaster.ReadAndDesToLString($@"{userFoldersPath}\{nick}\invitation.json");
            //AllChats = new List<List<string>> { ChatsWithPeople, AllPeople, SecretGroups, UserGroups, PublicGroups, Invitations }
            //.SelectMany(x => x)
            //.ToList();
            //AllGroups = new List<List<string>> { SecretGroups, UserGroups, PublicGroups, Invitations }
            //.SelectMany(x => x)
            //.ToList();
            //return new List<string>[] { ChatsWithPeople, AllPeople, SecretGroups, UserGroups, PublicGroups, Invitations };
        }
    }
}
