using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    class PersonChat
    {
        public PersonChat(string[] nicknames, string nameChat)
        {
            this.Nicknames = nicknames;
            this.NameChat = nameChat;
        }
        public string[] Nicknames;
        public string NameChat;
    }
}
