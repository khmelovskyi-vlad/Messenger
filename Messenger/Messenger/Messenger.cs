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
        private List<string> online = new List<string>();
        private object locketOnline = new object();
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
            Connector connector = new Connector(listener);
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
                lock (locketOnline)
                {
                    online.Add(nickname);
                }
                GroupMaster groupMaster = new GroupMaster(listener, nickname);
                var userInformation = await groupMaster.Run();
                if (userInformation.Length == 3 && userInformation[0] != "")
                {
                    await OpenCreateChat(listener, nickname, userInformation);
                }
                lock (locketOnline)
                {
                    online.Remove(nickname);
                }
                return true;
            }
            return false;
        }
        private bool CheckOnline(string nickname, Socket listener)
        {
            foreach (var user in online)
            {
                if (user == nickname)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task OpenCreateChat(Socket listener, string nick, string[] information)
        {
            var user = CreateUser(listener, nick);
            var findChat = false;
            foreach (var chat in chats)
            {
                if (information[1] == chat.NameChat)
                {
                    findChat = true;
                    await chat.Run(user, false);
                    CheckOnline(chat);
                    break;
                }
            }
            if (!findChat)
            {
                Chat chat = new Chat(user, information[0], information[1], information[2]);
                chats.Add(chat);
                await chat.Run(user, true);
                CheckOnline(chat);
            }
        }
        private void CheckOnline(Chat chat)
        {
            if (chat.UsersOnline.Count == 0)
            {
                chats.Remove(chat);
            }
        }
        private User CreateUser(Socket listener, string nick)
        {
            return new User(listener, nick);
        }
    }
}
