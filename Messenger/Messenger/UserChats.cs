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

        public List<string> ChatsWithPeople;
        public List<string> AllElsePeople;
        public List<string> SecretGroups;
        public List<string> UserGroups;
        public List<string> PublicGroups;
        public List<string> Invitations;
        public async Task<List<PersonChat>> FindPersonChats()
        {
            return (await fileMaster.ReadAndDeserialize<PersonChat>($@"{userFoldersPath}\{nick}\peopleChatsBeen.json")) ?? new List<PersonChat>();
        }
        public async Task<List<string>> FindInvitations()
        {
            Invitations = await fileMaster.ReadAndDeserialize<string>($@"{userFoldersPath}\{nick}\invitation.json");
            return Invitations;
        }
        public async Task<List<string>> FindUserGroups()
        {
            UserGroups = await fileMaster.ReadAndDeserialize<string>($@"{userFoldersPath}\{nick}\userGroups.json");
            return UserGroups;
        }
        public async Task<List<string>> FindAllElsePeople()
        {
            await FindChatsWithPeople();
            AllElsePeople = ((await fileMaster.ReadAndDeserialize<UserNicknameAndPasswordAndIPs>($@"D:\temp\messenger\nicknamesAndPasswords\users.json"))
                ?? new List<UserNicknameAndPasswordAndIPs>())
                .Select(x => x.Nickname)
                .Where(x => x != nick)
                .Except(ChatsWithPeople)
                .ToList();
            return AllElsePeople;
        }
        public async Task<List<string>> FindChatsWithPeople()
        {
            ChatsWithPeople = ((await fileMaster.ReadAndDeserialize<PersonChat>($@"{userFoldersPath}\{nick}\peopleChatsBeen.json"))
                ?? new List<PersonChat>())
                .Select(chat => chat.Nicknames[0] != nick ? chat.Nicknames[0] : chat.Nicknames[1])
                .ToList();
            return ChatsWithPeople;
        }
        public async Task<List<string>> FindSecretGroups()
        {
            SecretGroups = await fileMaster.ReadAndDeserialize<string>($@"{userFoldersPath}\{nick}\secretGroups.json");
            return SecretGroups;
        }
        public List<string> FindPublicGroups()
        {
            PublicGroups = fileMaster.GetDirectories(@"D:\temp\messenger\publicGroup")
                .Select(path => fileMaster.GetFileName(path))
                .ToList();
            return PublicGroups;
        }
        public async Task FindAllChats()
        {
            //await FindChatsWithPeople();
            await FindAllElsePeople();
            await FindSecretGroups();
            await FindUserGroups();
            FindPublicGroups();
            await FindInvitations();
        }
    }
}
