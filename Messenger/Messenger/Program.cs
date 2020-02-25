using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

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
        static async Task<int> Main(string[] args)
        {
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
