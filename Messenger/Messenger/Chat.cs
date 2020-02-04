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
                "If you want delete user, write: ?/delete user\n\r" +
                "If you want send file, write: ?/send\n\r" +
                "If you want download file, write: ?/download\n\r", user.Socket);
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
                else if (message == "?/send")
                {
                    message = await ReciveFile();
                }
                else if (message == "?/download")
                {
                    SendFile();
                    continue;
                }
                SendMessage(user, message);
            }
        }
        private void SendFile()
        {
            UsersOnline.Remove(user);
            user.communication.SendMessage("ok");
            user.communication.AnswerClient();
            var fileName = user.communication.data.ToString();
            var filesPaths = Directory.GetFiles(pathChat);
            foreach (var filePath in filesPaths)
            {
                var file = Path.GetFileName(filePath);
                if (file.Length > 19)
                {
                    var fileWithoutData = file.Remove(0, 19);
                    if (fileWithoutData == fileName)
                    {
                        user.communication.SendMessage("Finded");
                        user.communication.AnswerClient();
                        user.Socket.SendFile(filePath);
                        UsersOnline.Add(user);
                        return;
                    }
                }
            }
            user.communication.SendMessage("Didn`t find");
            UsersOnline.Add(user);
        }
        private async Task<string> ReciveFile()
        {
            UsersOnline.Remove(user);
            user.communication.SendMessage("Check your file");
            user.communication.AnswerClient();
            var nameFile = user.communication.data.ToString();
            user.communication.SendMessage("Ok");
            var timeString = DateTime.Now.ToString();
            StringBuilder normalTime = new StringBuilder();
            foreach (var timeChar in timeString)
            {
                if (timeChar == ':')
                {
                    normalTime.Append('.');
                    continue;
                }
                normalTime.Append(timeChar);
            }
            var filePath = $@"{pathChat}\\{normalTime}{nameFile}";
            await WriteFile(filePath);
            UsersOnline.Add(user);
            return nameFile;
        }
        public async Task WriteFile(string path)
        {
            buffer = new byte[size];
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                do
                {
                    var size = user.Socket.Receive(buffer);
                    await stream.WriteAsync(buffer, 0, size);
                } while (user.Socket.Available > 0);
            }
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
        private void CheckFile()
        {

        }
        private void SendMessage(User user, string message)
        {
            //var now = DateTime.Now.ToString();
            var messageSend = $"{user.Nickname}: {message}\n\r{DateTime.Now.ToString()}";
            SendMessageAllUsers(user, messageSend);
            lock (lockObj)
            {
                SaveMessage(messageSend);
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
    }
}
