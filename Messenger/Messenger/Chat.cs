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
    class Chat
    {
        public Chat(User user, string chatType, string nameChat, string pathChat)
        {
            this.user = user;
            this.ChatType = chatType;
            this.NameChat = nameChat;
            this.pathChat = pathChat;
        }
        public string NameChat { get; set; }

        public List<User> UsersOnline = new List<User>();
        private User user { get; }
        private string ChatType { get; }
        private StringBuilder data;
        private byte[] buffer;
        const int size = 256;
        private object lockObj = new object();
        private List<string> messages;
        private string pathChat;


        public async Task Run(User user, bool firstConnect)
        {
            user.communication.SendMessage("If you want exit, write: ?/end\n\r" +
                "If you want leave a group, write: ?/leave a group\n\r" +
                "If you want delete user, write: ?/delete user", user.Socket);
            user.communication.AnswerClient(user.Socket);
            if (firstConnect)
            {
                messages = await FirstRead();
            }
            FirstSentMessage(messages, user);
            UsersOnline.Add(user);
            while (true)
            {
                user.communication.AnswerClient(user.Socket);
                var message = user.communication.data.ToString();
                if (message == "?/end")
                {
                    UsersOnline.Remove(user);
                    return;
                }
                else if (message == "?/leave a group")
                {
                    GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, pathChat, ChatType, user.communication, NameChat);
                    if (await groupsLeaver.Leave())
                    {
                        return;
                    }
                }
                else if (message == "?/delete user")
                {
                    await DeleteUser();
                }
                else if (message == "?/send file")
                {
                    SendFile();
                }
                SendMessage(user);
            }
        }
        private void SendFile()
        {

        }
        private async Task DeleteUser()
        {
            while (true)
            {
                user.communication.SendMessage("Write user nickname");
                user.communication.AnswerClient(user.Socket);
                var nickname = user.communication.data.ToString();
                if (await CheckHavingNick(nickname))
                {
                    GroupsLeaver groupsLeaver = new GroupsLeaver(user.Nickname, pathChat, ChatType, user.communication, NameChat);
                    if (await groupsLeaver.Leave())
                    {
                        user.communication.SendMessage("Deleted this user");
                        foreach (var userOnline in UsersOnline)
                        {
                            if (userOnline.Nickname == nickname)
                            {
                                userOnline.communication.SendMessage("?/delete user");
                            }
                        }
                        return;
                    }
                }
                else
                {
                    user.communication.SendMessage("Don`t have this nickname");
                }
            }
        }
        private async Task<bool> CheckHavingNick(string nickname)
        {
            List<string> users;
            using (var stream = File.Open($"{pathChat}\\users.json", FileMode.Open, FileAccess.Read))
            {
                var usersJson = await ReadFile(stream);
                users = JsonConvert.DeserializeObject<List<string>>(usersJson.ToString());
            }
            foreach (var user in users)
            {
                if (user == nickname)
                {
                    return true;
                }
            }
            return false;
        }
        private void FirstSentMessage(List<string> messages, User user)
        {
            user.communication.SendMessage(messages.Count.ToString(), user.Socket);
            user.communication.AnswerClient(user.Socket);
            foreach (var message in messages)
            {
                user.communication.SendMessage(message, user.Socket);
                user.communication.AnswerClient(user.Socket);
            }
        }
        private void SendMessage(User user)
        {
            //var now = DateTime.Now.ToString();
            var message = $"{user.Nickname}: {user.communication.data.ToString()}\n\r{DateTime.Now.ToString()}";
            SendMessageAllUsers(user, message);
            lock (lockObj)
            {
                SaveMessage(message);
            }
        }
        private void SendMessageAllUsers(User user, string message)
        {
            foreach (var userOnline in UsersOnline)
            {
                if (userOnline.Nickname != user.Nickname)
                {
                    userOnline.communication.SendMessage(message, userOnline.Socket);
                }
            }
        }
        private async void SaveMessage(string message)
        {
            using (var stream = File.Open($"{pathChat}\\data.json", FileMode.Open, FileAccess.Write))
            {
                messages.Add(message);
                var messagesJson = JsonConvert.SerializeObject(messages);
                var buffer = Encoding.Default.GetBytes(messagesJson);
                stream.Seek(0, SeekOrigin.Begin);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
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
        private async Task<List<string>> FirstRead()
        {
            using (var stream = File.Open($"{pathChat}\\data.json", FileMode.OpenOrCreate, FileAccess.Read))
            {
                var messagesJson = await ReadFile(stream);
                var messages = JsonConvert.DeserializeObject<List<string>>(messagesJson.ToString());
                if (messages == null)
                {
                    messages = new List<string>();
                }
                return messages;
            }
        }




        AutoResetEvent resetSend = new AutoResetEvent(false);
        AutoResetEvent resetReceive = new AutoResetEvent(false);
        private void AnswerClient(Socket listener)
        {
            buffer = new byte[size];
            data = new StringBuilder();
            do
            {
                listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallback, listener);
                resetReceive.WaitOne();
            } while (listener.Available > 0);
        }
        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                resetReceive.Set();
                return;
            }
            data.Append(Encoding.ASCII.GetString(buffer, 0, received));
            resetReceive.Set();
        }
        private void SendMessage(string message, Socket listener)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
            resetSend.WaitOne();
        }
        private void SendCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            try
            {
                current.EndSend(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Can`t send message");
            }
            resetSend.Set();
        }
    }
}
