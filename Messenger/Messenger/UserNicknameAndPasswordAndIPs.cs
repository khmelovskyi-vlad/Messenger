using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    struct UserNicknameAndPasswordAndIPs
    {
        public UserNicknameAndPasswordAndIPs(string nickname, string password, List<string> IPs)
        {
            this.Nickname = nickname;
            this.Password = password;
            this.IPs = IPs;
        }
        public List<string> IPs;
        public string Nickname;
        public string Password;
    }
}
