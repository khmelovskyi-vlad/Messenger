using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    static class CharacterCheckers
    {
        public static bool CheckInput(string message)
        {
            foreach (var symbol in message)
            {
                //if (symbol >= '0' || symbol <= '9' || symbol >= 'a' || symbol <= 'z')
                //{
                //}
                if ((symbol < '0' || symbol > '9') && (symbol < 'a' || symbol > 'z'))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
