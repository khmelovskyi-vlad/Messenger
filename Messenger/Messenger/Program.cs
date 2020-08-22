using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Net;

namespace Messenger
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            //var i = BitConverter.GetBytes('i');
            //var l = BitConverter.GetBytes('l');
            //var s = BitConverter.GetBytes('s');
            //var o = 5;
            //var k = 6;
            //if (o == 5)
            //{
            //    Console.WriteLine(5);
            //}
            //else if (k == 6)
            //{
            //    Console.WriteLine(6);
            //}
            //else
            //{
            //    Console.WriteLine(0);
            //}
            //char k = '3';
            //var l = BitConverter.GetBytes(true);
            //var l23 = BitConverter.GetBytes(false);
            //var l2 = BitConverter.GetBytes(k);
            //byte[] l3 = new byte[3];
            //Array.Copy(l2, 0, l3, 0, l2.Length);
            //l3[2] = l[0];
            ////byte[] l3 = new byte[3] { l[0], l2[0], l2[1] };

            //var kljl = BitConverter.ToBoolean(l3, 0);


            var mainDirectoryPath = @"D:\temp\messenger";
            ProjectPaths.MainDirectoryPath = mainDirectoryPath;
            Server server = new Server(mainDirectoryPath);
            await server.Run(5);
            return 1;
        }
    }
    static class ProjectPaths
    {
        static public string MainDirectoryPath { get; set; }
        static public string BansPath { get { return Path.Combine(MainDirectoryPath, "bans"); } }
        static public string NicknamesAndPasswordsPath { get { return Path.Combine(MainDirectoryPath, "nicknamesAndPasswords"); } }
        static public string PeopleChatsPath { get { return Path.Combine(MainDirectoryPath, "peopleChats"); } }
        static public string PublicGroupPath { get { return Path.Combine(MainDirectoryPath, "publicGroup"); } }
        static public string SecretGroupPath { get { return Path.Combine(MainDirectoryPath, "secretGroup"); } }
        static public string UsersPath { get { return Path.Combine(MainDirectoryPath, "Users"); } }
    }
}