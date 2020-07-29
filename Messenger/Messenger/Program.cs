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
            Server server = new Server();
            await server.Run(5);
            return 1;
        }
    }
}