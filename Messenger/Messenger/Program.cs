using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;

namespace Messenger
{
    class Program
    {
        private static int age = 32;
        
        static void Main(string[] args)
        {
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
            Server server = new Server();
            server.Run(5);
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
