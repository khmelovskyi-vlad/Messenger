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