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
        public GroupsLeaver(string user, string pathChat, string typeChat, Communication communication, string nameChat)
        {
            this.NameChat = nameChat;
            this.communication = communication;
            this.user = user;
            this.pathChat = pathChat;
            this.typeChat = typeChat;
        }
        private string NameChat { get; }
        private Communication communication;
        private string user;
        private string pathChat;
        private string typeChat;
        private PersonChat needChat;
        public async Task<bool> Leave()
        {
            var pathElements = await FindPathsElement();
            if (!await DeleteData(pathElements))
            {
                await AddData(pathElements);
            }
            communication.SendMessage("You leave a chat");
            return true;
            //communication.SendMessage("You really want to leave a group? If yes write: 'yes'");
            //communication.AnswerClient();
            //var pathElements = FindPathsElement();
            //if (communication.data.ToString() == "Yes")
            //{
            //    await DeleteData(pathElements);
            //    await AddData(pathElements);
            //    communication.SendMessage("You leave a chat");
            //    return true;
            //}
            //else
            //{
            //    communication.SendMessage("Ok, you did not to leave chat");
            //    return false;
            //}
        }
        private async Task<string[]> FindPathsElement()
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
            var users = new List<string>();
            using (var stream = File.Open($"{pathChat}\\users.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var usersJsonSb = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJsonSb.ToString());
            }
            users.Remove(user);
            if (users.Count == 0)
            {
                await DeleteGroup(pathsElement);
                needDeleteGroup = true;
            }
            var usersJson = JsonConvert.SerializeObject(users);
            await WriteData($"{pathChat}\\users.json", usersJson);

            if (pathsElement[0] == "peopleChatsBeen")
            {
                await DeletePeopleChatsBeen("peopleChatsBeen");
                return needDeleteGroup;
            }

            var nameChats = new List<string>();
            using (var stream = File.Open($@"D:\temp\messenger\Users\{user}\{pathsElement[0]}.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var nameChatsJsonSb = await ReadFile(stream);
                nameChats = JsonConvert.DeserializeObject<List<string>>(nameChatsJsonSb.ToString());
            }
            nameChats.Remove(NameChat);
            var nameChatsJson = JsonConvert.SerializeObject(nameChats);
            await WriteData($@"D:\temp\messenger\Users\{user}\{pathsElement[0]}.json", nameChatsJson);
            return needDeleteGroup;
        }
        private async Task DeletePeopleChatsBeen(string pathsElement)
        {
            List<PersonChat> nameChats;
            using (var stream = File.Open($@"D:\temp\messenger\Users\{user}\{pathsElement}.json", FileMode.OpenOrCreate, FileAccess.Read))
            {
                var nameChatsJsonSb = await ReadFile(stream);
                nameChats = JsonConvert.DeserializeObject<List<PersonChat>>(nameChatsJsonSb.ToString());
            }
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
                var nameChatsJson = JsonConvert.SerializeObject(nameChats);
                await WriteData($@"D:\temp\messenger\Users\{user}\{pathsElement}.json", nameChatsJson);
            }
        }
        private async Task AddData(string[] pathsElement)
        {
            using (var stream = File.Open($"{pathChat}\\leavedPeople.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var usersJson = await ReadFile(stream);
                var users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
                if (users == null)
                {
                    users = new List<string>();
                }
                users.Add(user);
                await Write(stream, users);
            }
            if (pathsElement[0] == "peopleChatsBeen")
            {
                await AddPeopleChatsBeen("leavedPeopleChatsBeen");
                return;
            }
            using (var stream = File.Open($@"D:\temp\messenger\Users\{user}\{pathsElement[1]}.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var usersJson = await ReadFile(stream);
                var users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
                if (users == null)
                {
                    users = new List<string>();
                }
                users.Add(NameChat);
                await Write(stream, users);
            }
        }
        private async Task AddPeopleChatsBeen(string pathsElement)
        {
            List<PersonChat> nameChats;
            using (var stream = File.Open($@"D:\temp\messenger\Users\{user}\{pathsElement}.json", FileMode.OpenOrCreate, FileAccess.Read))
            {
                var nameChatsJsonSb = await ReadFile(stream);
                nameChats = JsonConvert.DeserializeObject<List<PersonChat>>(nameChatsJsonSb.ToString());
            }
            if (nameChats == null)
            {
                nameChats = new List<PersonChat>();
            }
            nameChats.Add(needChat);
            var nameChatsJson = JsonConvert.SerializeObject(nameChats);
            await WriteData($@"D:\temp\messenger\Users\{user}\{pathsElement}.json", nameChatsJson);
        }
        private async Task Write(FileStream stream, List<string> users)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var usersJson = JsonConvert.SerializeObject(users);
            var buffer = Encoding.Default.GetBytes(usersJson);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
        private async Task<StringBuilder> ReadFile(FileStream stream)
        {
            StringBuilder usersJson = new StringBuilder();
            var buffer = 256;
            var arrayBytes = new byte[buffer];
            while (true)
            {
                var readedRealBytes = await stream.ReadAsync(arrayBytes, 0, buffer);
                usersJson.Append(Encoding.Default.GetString(arrayBytes, 0, readedRealBytes));
                if (readedRealBytes < buffer)
                {
                    break;
                }
            }
            return usersJson;
        }
        private async Task DeleteGroup(string[] pathsElement)
        {


            //var leavedPathss = new List<string>();
            //using (var stream = File.Open($"{pathChat}\\leavedPeople.json", FileMode.OpenOrCreate, FileAccess.Read))
            //{
            //    var usersJson = await ReadFile(stream);
            //    var users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            //    if (users == null)
            //    {
            //        return;
            //    }
            //    foreach (var user in users)
            //    {
            //        leavedPathss.Add($@"D:\temp\messenger\Users\{user}");
            //    }
            //}
            //foreach (var leavedPath in leavedPathss)
            //{
            //    var path = $"{leavedPath}\\{pathsElement[1]}.json";
            //    var users = new List<string>();
            //    using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
            //    {
            //        var usersJson = await ReadFile(stream);
            //        users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            //    }
            //    users.Remove(NameChat);
            //    var usersJsonNew = JsonConvert.SerializeObject(users);
            //    await WriteData(path, usersJsonNew);
            //}
            //var invitationPathss = new List<string>();
            //using (var stream = File.Open($"{pathChat}\\invitation.json", FileMode.OpenOrCreate, FileAccess.Read))
            //{
            //    var usersJson = await ReadFile(stream);
            //    var users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            //    if (users == null)
            //    {
            //        return;
            //    }
            //    foreach (var user in users)
            //    {
            //        invitationPathss.Add($@"D:\temp\messenger\Users\{user}\invitation.json");
            //    }
            //}
            var invitationName = "";
            if (typeChat == "pg" || typeChat == "ug")
            {
                invitationName = $"public: {NameChat}";
            }
            else if (typeChat == "sg")
            {
                invitationName = $"secret: {NameChat}";
            }
            //foreach (var invitationPath in invitationPathss)
            //{
            //    var users = new List<string>();
            //    using (var stream = File.Open(invitationPath, FileMode.OpenOrCreate, FileAccess.Read))
            //    {
            //        var usersJson = await ReadFile(stream);
            //        users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            //    }
            //    users.Remove(invitationName);
            //    var usersJsonNew = JsonConvert.SerializeObject(users);
            //    await WriteData(invitationPath, usersJsonNew);
            //}
            var invitationPaths = await FindUserPath($"{pathChat}\\invitation.json", "\\invitation.json");
            if (invitationPaths != null)
            {
                await DeleteExtraData(invitationPaths, invitationName);
            }
            var leavedPaths = await FindUserPath($"{pathChat}\\leavedPeople.json", $"\\{pathsElement[1]}.json");
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
                List<PersonChat> nameChats;
                using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    var usersJson = await ReadFile(stream);
                    nameChats = JsonConvert.DeserializeObject<List<PersonChat>>(usersJson.ToString());
                }
                foreach (var nameChat in nameChats)
                {
                    if (nameChat.NameChat == NameChat)
                    {
                        needChat = nameChat;
                        break;
                    }
                }
                nameChats.Remove(needChat);
                var usersJsonNew = JsonConvert.SerializeObject(nameChats);
                await WriteData(path, usersJsonNew);
            }
        }
        private async Task DeleteExtraData(List<string> paths, string nameChat)
        {
            foreach (var path in paths)
            {
                var users = new List<string>();
                using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    var usersJson = await ReadFile(stream);
                    users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
                }
                users.Remove(nameChat);
                var usersJsonNew = JsonConvert.SerializeObject(users);
                await WriteData(path, usersJsonNew);
            }
        }
        private async Task<List<string>> FindUserPath(string path, string lastPartOfPath)
        {
            var leavedPaths = new List<string>();
            List<string> users;
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
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
        private async Task WriteData(string path, string data)
        {
            using (var stream = new StreamWriter(path, false))
            {
                await stream.WriteAsync(data);
            } 
        }
    }
}
