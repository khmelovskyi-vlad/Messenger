using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class PersonChat
    {
        public PersonChat(string[] nickname, string nameChat)
        {
            this.Nicknames = nickname;
            this.NameChat = nameChat;
        }
        public string[] Nicknames;
        public string NameChat;
    }
}
