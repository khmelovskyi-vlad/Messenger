using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    struct GroupInformation
    {
        public GroupInformation(bool canOpenChat, string type, string name, string path)
        {
            this.CanOpenChat = canOpenChat;
            this.Type = type;
            this.Name = name;
            this.Path = path;
        }
        public bool CanOpenChat;
        public string Type;
        public string Name;
        public string Path;
    }
}
