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
        public GroupsLeaver(string user, string pathChat, string typeChat, string nameChat)
        {
            this.NameChat = nameChat;
            this.user = user;
            this.pathChat = pathChat;
            this.typeChat = typeChat;
        }
        private string NameChat { get; }
        private string user;
        private string pathChat;
        private string typeChat;
        private PersonChat needChat;
        FileMaster fileMaster = new FileMaster();
        public async Task<bool> Leave()
        {
            var pathElements = await Task.Run(() => FindPathsElement());
            if (!await DeleteData(pathElements))
            {
                await AddData(pathElements);
            }
            //communication.SendMessage("You leave a chat");
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
            await fileMaster.ReadWrite($"{pathChat}\\users.json", users =>
            {
                users.Remove(user);
                if (users.Count == 0)
                {
                    needDeleteGroup = true;
                }
                return (users, true);
            });
            if (needDeleteGroup)
            {
                await DeleteGroup(pathsElement);
            }

            if (pathsElement[0] == "peopleChatsBeen")
            {
                await DeletePeopleChatsBeen("peopleChatsBeen");
            }
            else
            {
                await fileMaster.ReadWrite($@"D:\temp\messenger\Users\{user}\{pathsElement[0]}.json", nameChats =>
                {
                    nameChats.Remove(NameChat);
                    return (nameChats, true);
                });
            }
            return needDeleteGroup;
        }
        private async Task DeletePeopleChatsBeen(string pathsElement)
        {
            await fileMaster.ReadWrite($@"D:\temp\messenger\Users\{user}\{pathsElement}.json", nameChats =>
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
            await fileMaster.ReadWrite($"{pathChat}\\leavedPeople.json", users =>
            {
                if (users == null)
                {
                    users = new List<string>();
                }
                users.Add(user);
                return (users, true);
            });
            if (pathsElement[0] == "peopleChatsBeen")
            {
                await AddPeopleChatsBeen("leavedPeopleChatsBeen");
            }
            else
            {
                await fileMaster.ReadWrite($@"D:\temp\messenger\Users\{user}\{pathsElement[1]}.json", nameChats =>
                {
                    if (nameChats == null)
                    {
                        nameChats = new List<string>();
                    }
                    nameChats.Add(NameChat);
                    return (nameChats, true);
                });
            }
        }
        private async Task AddPeopleChatsBeen(string pathsElement)
        {
            await fileMaster.ReadWrite($@"D:\temp\messenger\Users\{user}\{pathsElement}.json", nameChats =>
            {
                if (nameChats == null)
                {
                    nameChats = new List<PersonChat>();
                }
                nameChats.Add(needChat);
                return (nameChats, true);
            });
        }
        private async Task DeleteGroup(string[] pathsElement)
        {


            GroupDeleter groupDeleter = new GroupDeleter(NameChat, pathChat, typeChat, fileMaster);
            await groupDeleter.Run();
            ////var leavedPathss = new List<string>();
            ////using (var stream = File.Open($"{pathChat}\\leavedPeople.json", FileMode.OpenOrCreate, FileAccess.Read))
            ////{
            ////    var usersJson = await ReadFile(stream);
            ////    var users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            ////    if (users == null)
            ////    {
            ////        return;
            ////    }
            ////    foreach (var user in users)
            ////    {
            ////        leavedPathss.Add($@"D:\temp\messenger\Users\{user}");
            ////    }
            ////}
            ////foreach (var leavedPath in leavedPathss)
            ////{
            ////    var path = $"{leavedPath}\\{pathsElement[1]}.json";
            ////    var users = new List<string>();
            ////    using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read))
            ////    {
            ////        var usersJson = await ReadFile(stream);
            ////        users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            ////    }
            ////    users.Remove(NameChat);
            ////    var usersJsonNew = JsonConvert.SerializeObject(users);
            ////    await WriteData(path, usersJsonNew);
            ////}
            ////var invitationPathss = new List<string>();
            ////using (var stream = File.Open($"{pathChat}\\invitation.json", FileMode.OpenOrCreate, FileAccess.Read))
            ////{
            ////    var usersJson = await ReadFile(stream);
            ////    var users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            ////    if (users == null)
            ////    {
            ////        return;
            ////    }
            ////    foreach (var user in users)
            ////    {
            ////        invitationPathss.Add($@"D:\temp\messenger\Users\{user}\invitation.json");
            ////    }
            ////}
            //var invitationName = "";
            //if (typeChat == "pg" || typeChat == "ug")
            //{
            //    invitationName = $"public: {NameChat}";
            //}
            //else if (typeChat == "sg")
            //{
            //    invitationName = $"secret: {NameChat}";
            //}
            ////foreach (var invitationPath in invitationPathss)
            ////{
            ////    var users = new List<string>();
            ////    using (var stream = File.Open(invitationPath, FileMode.OpenOrCreate, FileAccess.Read))
            ////    {
            ////        var usersJson = await ReadFile(stream);
            ////        users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            ////    }
            ////    users.Remove(invitationName);
            ////    var usersJsonNew = JsonConvert.SerializeObject(users);
            ////    await WriteData(invitationPath, usersJsonNew);
            ////}
            //var invitationPaths = await FindUserPath($"{pathChat}\\invitation.json", "\\invitation.json");
            //if (invitationPaths != null)
            //{
            //    await DeleteExtraData(invitationPaths, invitationName);
            //}
            //var leavedPaths = await FindUserPath($"{pathChat}\\leavedPeople.json", $"\\{pathsElement[1]}.json");
            //if (leavedPaths != null)
            //{
            //    if (typeChat == "pp" || typeChat == "ch")
            //    {
            //        await DeleteLeavedPeople(leavedPaths);
            //        return;
            //    }
            //    await DeleteExtraData(leavedPaths, NameChat);
            //}
        }
        //private async Task DeleteLeavedPeople(List<string> paths)
        //{
        //    foreach (var path in paths)
        //    {
        //        await fileMaster.ReadWrite(path, nameChats =>
        //        {
        //            foreach (var nameChat in nameChats)
        //            {
        //                if (nameChat.NameChat == NameChat)
        //                {
        //                    needChat = nameChat;
        //                    break;
        //                }
        //            }
        //            nameChats.Remove(needChat);
        //            return (nameChats, true);
        //        });
        //    }
        //}
        //private async Task DeleteExtraData(List<string> paths, string nameChat)
        //{
        //    foreach (var path in paths)
        //    {
        //        await fileMaster.ReadWrite(path, users =>
        //        {
        //            users.Remove(nameChat);
        //            return (users, true);
        //        });
        //    }
        //}
        //private async Task<List<string>> FindUserPath(string path, string lastPartOfPath)
        //{
        //    var leavedPaths = new List<string>();
        //    var users = await fileMaster.ReadAndDesToLString(path);
        //    if (users == null)
        //    {
        //        return null;
        //    }
        //    foreach (var user in users)
        //    {
        //        leavedPaths.Add($@"D:\temp\messenger\Users\{user}{lastPartOfPath}");
        //    }
        //    return leavedPaths;
        //}
    }
}
