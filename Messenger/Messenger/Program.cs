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

namespace Messenger
{
    class Program
    {
        private static int age = 32;
        public static bool TryTo(string path)
        {
            bool result = false;
            Tuple<AutoResetEvent, FileSystemWatcher> tuple = null;

            while (true)
            {
                try
                {
                    using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Write))
                    {
                        var buffer = Encoding.Default.GetBytes("test");
                        file.Write(buffer, 0, buffer.Length);
                        result = true;
                        break;
                    }
                }
                catch (IOException ex)
                {
                    // Init only once and only if needed. Prevent against many instantiation in case of multhreaded 
                    // file access concurrency (if file is frequently accessed by someone else). Better memory usage.
                    if (tuple == null)
                    {
                        var autoResetEvent = new AutoResetEvent(true);
                        var dir = Path.GetDirectoryName(path);
                        var fileSystemWatcher = new FileSystemWatcher(dir)
                        {
                            EnableRaisingEvents = true
                        };
                        fileSystemWatcher.Filter = Path.GetFileName(path);
                        var secondd = Path.GetFullPath(path);
                        fileSystemWatcher.Changed +=
                            (o, e) =>
                            {
                                Console.WriteLine(e.Name);
                                Console.WriteLine(e.FullPath);
                                var firts = Path.GetFullPath(e.FullPath);
                                var second = Path.GetFullPath(path);
                                if (firts == second)
                                {
                                    autoResetEvent.Set();
                                    if (tuple != null && tuple.Item1 != null) // Dispose of resources now (don't wait the GC).
                                    {
                                        tuple.Item1.Dispose();
                                        tuple.Item2.Dispose();
                                    }
                                }
                            };

                        tuple = new Tuple<AutoResetEvent, FileSystemWatcher>(autoResetEvent, fileSystemWatcher);
                    }
                    

                    tuple.Item1.WaitOne(-1);
                }
            }

            if (tuple != null && tuple.Item1 != null) // Dispose of resources now (don't wait the GC).
            {
                tuple.Item1.Dispose();
                tuple.Item2.Dispose();
            }

            return result;
        }
        static void serverr()
        {
            Process adder = new Process();
            adder.StartInfo.FileName = "Messenger.exe";
            adder.StartInfo.UseShellExecute = false;
            adder.StartInfo.RedirectStandardOutput = true;
            using (AnonymousPipeServerStream serverPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable, 100))
            {
                adder.StartInfo.Arguments = serverPipe.GetClientHandleAsString();//arguments are passed thru the funnel extended by client.
                adder.Start();
                serverPipe.DisposeLocalCopyOfClientHandle();
                serverPipe.Write(System.Text.Encoding.UTF8.GetBytes("3 4"), 0, System.Text.Encoding.UTF8.GetByteCount("3 4"));
            }
            StreamReader reader = adder.StandardOutput;
            Console.WriteLine("Sever:Adder:{0}", reader.ReadToEnd());
            adder.WaitForExit();
        }
        private async static Task TestPipeMessenger()
        {
            Process pipeClient = new Process();
            pipeClient.StartInfo.FileName = "Messenger.exe";
            pipeClient.StartInfo.Arguments = "1";
            pipeClient.Start();

            using (NamedPipeServerStream pipeServer =
                new NamedPipeServerStream("testpipe", PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {
                Console.WriteLine("NamedPipeServerStream object created.");

                // Wait for a client to connect
                Console.Write("Waiting for client connection...");
                pipeServer.WaitForConnection();

                Console.WriteLine("Client connected.");
                try
                {
                    // Read user input and send that to the client process.
                    using (StreamWriter sw = new StreamWriter(pipeServer))
                    {
                        sw.AutoFlush = true;
                        while (true)
                        {
                            Console.Write("Enter text: ");
                            var line = Console.ReadLine();
                            await sw.WriteLineAsync(line);
                            if (line == "all")
                            {
                                break;
                            }
                        }
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e)
                {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }
            }
        }
        private async static Task TestPipeClient(string[] args)
        {
            using (NamedPipeClientStream pipeClient =
                new NamedPipeClientStream(".", "testpipe", PipeDirection.In, PipeOptions.Asynchronous))
            {

                // Connect to the pipe or wait until the pipe is available.
                Console.Write("Attempting to connect to pipe...");
                pipeClient.Connect();

                Console.WriteLine("Connected to pipe.");
                Console.WriteLine("There are currently {0} pipe server instances open.",
                   pipeClient.NumberOfServerInstances);
                using (StreamReader sr = new StreamReader(pipeClient))
                {
                    // Display the read text to the console
                    string temp;
                    while ((temp = await sr.ReadLineAsync()) != null)
                    {
                        Console.WriteLine("Received from server: {0}", temp);
                    }
                }
            }
            Console.Write("Press Enter to continue...");
            Console.ReadLine();
        }
        static async Task<int> Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    await TestPipeMessenger();
            //}
            //else if (args[0] == "1")
            //{
            //    await TestPipeClient(args);
            //}
            var first = new int[] { 1, 2, 3, 4 };
            var second = new int[] { 2, 5 };
            var three = first.Except(second);
            foreach (var i in three)
            {
                Console.WriteLine(i);
            }
            var l = Directory.GetDirectories("D:\\temp\\zerro").DefaultIfEmpty().Where(x => x != "").ToList();
            var k = new List<string> { };
            k = null;
            if ((k ?? new List<string> { }).Contains("lol"))
            {
                Console.WriteLine("lol");
            }
            else
            {
                Console.WriteLine("no");
            }
            Console.ReadKey();
            //TryTo(@"D:\temp\ok3\test2.txt");
            //Console.WriteLine("next");
            //Console.ReadKey();
            //var list = new List<UserNicknameAndPasswordAndIPs>();
            //list.Add(new UserNicknameAndPasswordAndIPs("1", "2", new List<string> { "3" }));
            //list.Add(new UserNicknameAndPasswordAndIPs("1", "2", new List<string> { "3" }));
            //list.Add(new UserNicknameAndPasswordAndIPs("1", "2", new List<string> { "3" }));
            //FileMaster fileMaster = new FileMaster();
            //await fileMaster.WriteFile(@"D:\temp\ok3\test.json", list);
            //var listJson = await fileMaster.ReadFile(@"D:\temp\ok3\test.json");
            //var newList = JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(listJson);
            //foreach (var data in newList)
            //{
            //    Console.WriteLine(data.Nickname);
            //}
            //Console.ReadKey();
            //var timeString = DateTime.Now.ToString();
            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (var timeChar in timeString)
            //{
            //    if (timeChar == ':')
            //    {
            //        stringBuilder.Append('.');
            //        continue;
            //    }
            //    stringBuilder.Append(timeChar);
            //}
            //Directory.CreateDirectory($@"D:\temp\messenger\publicGroup\vlad{stringBuilder}");
            //File.Create($@"D:\temp\messenger\publicGroup\v{stringBuilder}.txt");
            //Console.ReadKey();
            //int[] s1 = { 1, 2, 3 };
            //int[] s2 = new int[s1.Length + 1];
            //Array.Copy(s1, 0, s1 = new int[s1.Length + 1], 1, s1.Length - 1);
            //Console.ReadKey();
            //List<string> list1 = new List<string>();
            //list1.Add("1");
            //List<string> list2 = new List<string>();
            //list2.Add("2");
            //List<string> list3 = new List<string>();
            //list3.Add("3");
            //List<List<string>> list4 = new List<List<string>>();
            //list4.Add(list1);
            //list4.Add(list2);
            //list4.Add(list3);
            //var jsonList = JsonConvert.SerializeObject(list4);
            //using (var stream = File.Open(@"D:\temp\messenger\vlad.json", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            //{
            //    var bytes = Encoding.Default.GetBytes(jsonList);
            //    stream.Write(bytes, 0, bytes.Length);
            //}
            //Console.ReadKey();
            //var s = "012345";
            //var newS = s.Remove(0, 2);
            //Console.WriteLine(newS);
            //Console.ReadKey();

            //var timeString = DateTime.Now.ToString();
            //StringBuilder normalTime = new StringBuilder();
            //foreach (var timeChar in timeString)
            //{
            //    if (timeChar == ':')
            //    {
            //        normalTime.Append('.');
            //        continue;
            //    }
            //    normalTime.Append(timeChar);
            //}
            Server server = new Server();
            await server.Run(5);
            return 1;
            Console.ReadKey();
            if (args.Length != 0)
            {
                if (args[0] == "1")
                {
                    AlternatePathOfExecution();
                }
                if (args[0] == "2")
                {
                    AlternatePathOfExecution();
                }
                //add other options here and below              
            }
            else
            {
                NormalPathOfExectution();
            }
        }
        private static void NormalPathOfExectution()
        {
            Console.WriteLine("Doing something here4");
            age = 1;
            //need one of these for each additional console window
            Process myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.FileName = "Messenger.exe";
            string[] s = new string[2];
            myProcess.StartInfo.Arguments = "1";
            myProcess.Start();
            myProcess.Start();
            myProcess.Start();
            myProcess.Start();
            Console.ReadLine();
            Console.WriteLine(age);
            var sv = myProcess.StandardInput;

        }
        private static void AlternatePathOfExecution()
        {
            Console.WriteLine("Write something different on other Console");
            Console.WriteLine(age);
            age++;
            Console.ReadLine();
        }
    }
}
