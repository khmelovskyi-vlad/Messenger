using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    class Messenger
    {
        public Messenger()
        {

        }
        public List<User> online = new List<User>();
        public object locketOnline = new object();
        private List<string> users;
        private StringBuilder data;
        private byte[] buffer;
        const int size = 256;
        private object obj = new object();
        private string UserFoldersPath { get { return @"D:\temp\messenger\Users"; } }
        private string PublicGroupsPath { get { return @"D:\temp\messenger\publicGroup"; } }
        private List<Chat> chats = new List<Chat>();
        public async Task<bool> Connect(Socket listener)
        {
            Connector connector = new Connector(listener, this);
            var nickname = await connector.Run();
            if (nickname == "?Disconnect")
            {
                return false;
            }
            if (nickname.Length != 0)
            {
                var isOnline = CheckOnline(nickname, listener);
                if (isOnline)
                {
                    return false;
                }
                var user = CreateUser(listener, nickname);
                lock (locketOnline)
                {
                    online.Add(user);
                }
                while (true)
                {
                    GroupMaster groupMaster = new GroupMaster(user);
                    var groupInformation = await groupMaster.Run();
                    if (groupInformation.CanOpenChat)
                    {
                        await OpenCreateChat(user, groupInformation);
                        if (CheckLeftMessanger(user))
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                lock (locketOnline)
                {
                    online.Remove(user);
                }
                return true;
            }
            return false;
        }
        private bool CheckLeftMessanger(User user)
        {
            user.communication.SendMessage("If you want left the messanger, write: exit");
            user.communication.AnswerClient();
            if (user.communication.data.ToString() == "exit")
            {
                user.communication.SendMessage("You left the messanger");
                return true;
            }
            else
            {
                user.communication.SendMessage("Ok, choose new chat");
                return false;
            }
        }
        private bool CheckOnline(string nickname, Socket listener)
        {
            lock (locketOnline)
            {
                foreach (var user in online)
                {
                    if (user.Nickname == nickname)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private async Task OpenCreateChat(User user, GroupInformation groupInformation)
        {
            var findChat = false;
            foreach (var chat in chats)
            {
                if (groupInformation.Name == chat.NameChat)
                {
                    findChat = true;
                    await chat.Run(user, false);
                    CheckOnline(chat);
                    break;
                }
            }
            if (!findChat)
            {
                Chat chat = new Chat(groupInformation.Type, groupInformation.Name, groupInformation.Path);
                chats.Add(chat);
                await chat.Run(user, true);
                CheckOnline(chat);
            }
        }
        private void CheckOnline(Chat chat)
        {
            if (chat.UsersOnline.Count == 0)
            {
                if (chat.UsersOnlineToCheck.Count == 0)
                {
                    chats.Remove(chat);
                }
            }
        }
        private User CreateUser(Socket listener, string nick)
        {
            return new User(listener, nick);
        }
    }
}
