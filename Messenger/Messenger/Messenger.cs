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
        public Messenger(Server server)
        {
            this.Server = server;
        }
        public Server Server;
        public List<User> online = new List<User>();
        public object OnlineLock = new object();
        private List<Chat> chats = new List<Chat>(); //needChatLock
        public async Task<bool> Connect(Socket socket)
        {
            var result = false;
            try
            {
                Connector connector = new Connector(socket, this);
                var nickname = await connector.Run();
                if (nickname == "?Disconnect")
                {
                    return false;
                }
                if (nickname.Length != 0)
                {
                    var isOnline = CheckOnline(nickname);
                    if (isOnline)
                    {
                        return false;
                    }
                    var user = CreateUser(socket, nickname);
                    lock (OnlineLock)
                    {
                        online.Add(user);
                    }
                    await FindUseChat(user);
                    lock (OnlineLock)
                    {
                        online.Remove(user);
                    }
                    return true;
                }
                return false;
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex);
                return result;
            }
            catch (Exception socketException)
            {
                Console.WriteLine(socketException);
                //throw socketException;
                return result;
            }
            finally
            {
                socket.Close();
                socket.Dispose();
            }
        }
        private async Task FindUseChat(User user)
        {
            try
            {
                while (true)
                {
                    GroupMaster groupMaster = new GroupMaster(user, this);
                    var groupInformation = await groupMaster.Run();
                    if (groupInformation.CanOpenChat)
                    {
                        await OpenCreateChat(user, groupInformation);
                        if (await CheckLeftMessanger(user))
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                lock (OnlineLock)
                {
                    online.Remove(user);
                }
                throw ex;
            }
        }
        private async Task<bool> CheckLeftMessanger(User user)
        {
            var result = true;
            await user.communication.AnswerClient();
            await user.communication.SendMessage("If you want to left the messanger, write: 'exit'");
            await user.communication.AnswerClient();
            if (user.communication.data.ToString() == "exit")
            {
                await user.communication.SendMessage("You left the messanger");
            }
            else
            {
                await user.communication.SendMessage("Ok, choose new chat");
                result = false;
            }
            await user.communication.AnswerClient();
            return result;
        }
        private bool CheckOnline(string nickname)
        {
            lock (OnlineLock)
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
                Chat chat = new Chat(groupInformation.Type, groupInformation.Name, groupInformation.Path, this);
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
        private User CreateUser(Socket socket, string nick)
        {
            return new User(socket, nick);
        }
    }
}
