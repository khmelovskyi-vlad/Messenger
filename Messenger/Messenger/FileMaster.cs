using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    class FileMaster
    {
        public FileMaster()
        {

        }

        public void DeleterFolder(string path)
        {
            Directory.Delete(path, true);
        }
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        public string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }
        public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }
        public async Task<List<UserNicknameAndPasswordAndIPs>> ReadAndDesToLUserInf(string path)
        {
            var dataJson = await ReadData(path);
            return DesToLUserInf(dataJson);
        }
        public async Task<List<string>> ReadAndDesToLString(string path)
        {
            var dataJson = await ReadData(path);
            return DesToLString(dataJson);
        }
        public async Task<List<PersonChat>> ReadAndDesToPersonCh(string path)
        {
            var dataJson = await ReadData(path);
            return DesToPersonCh(dataJson);
        }
        private List<PersonChat> DesToPersonCh(string dataJson)
        {
            return JsonConvert.DeserializeObject<List<PersonChat>>(dataJson);
        }
        private List<UserNicknameAndPasswordAndIPs> DesToLUserInf(string dataJson)
        {
            return JsonConvert.DeserializeObject<List<UserNicknameAndPasswordAndIPs>>(dataJson);
        }
        private List<string> DesToLString(string dataJson)
        {
            return JsonConvert.DeserializeObject<List<string>>(dataJson);
        }
        public string[] ReadUsersPaths()
        {
            return Directory.GetFiles(@"D:\temp\messenger\Users");
        }
        public async Task<string> ReadData(string path)
        {
            Tuple<AutoResetEvent, FileSystemWatcher> tuple = null;
            while (true)
            {
                try
                {
                    string dataJson;
                    using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Write))
                    {
                        dataJson = await ReadData(stream);
                    }
                    File.SetLastWriteTime(path, DateTime.Now);
                    return dataJson;
                }
                catch (IOException ex)
                {
                    if (tuple == null)
                    {
                        var autoResetEvent = new AutoResetEvent(true);
                        var fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(path))
                        {
                            EnableRaisingEvents = true
                        };
                        fileSystemWatcher.Filter = Path.GetFileName(path);
                        fileSystemWatcher.Changed +=
                            (o, e) =>
                            {
                                if (Path.GetFullPath(e.FullPath) == Path.GetFullPath(path))
                                {
                                    autoResetEvent.Set();
                                    Dispose(tuple);
                                }
                            };

                        tuple = new Tuple<AutoResetEvent, FileSystemWatcher>(autoResetEvent, fileSystemWatcher);
                    }
                    tuple.Item1.WaitOne(-1);
                }
            }
            Dispose(tuple);
        }
        private void Dispose(Tuple<AutoResetEvent, FileSystemWatcher> tuple)
        {
            if (tuple != null && tuple.Item1 != null) // Dispose of resources now (don't wait the GC).
            {
                tuple.Item1.Dispose();
                tuple.Item2.Dispose();
            }
        }
        private async Task<string> ReadData(FileStream stream)
        {
            var sb = new StringBuilder();
            var count = 256;
            var buffer = new byte[count];
            while (true)
            {
                var realCount = await stream.ReadAsync(buffer, 0, count);
                sb.Append(Encoding.Default.GetString(buffer, 0, realCount));
                if (realCount < count)
                {
                    break;
                }
            }
            return sb.ToString();
        }
        //public async Task WriteData(string path, object data)
        //{
        //    var dataJson = JsonConvert.SerializeObject(data);
        //    using (var stream = new StreamWriter(path, false))
        //    {
        //        await stream.WriteAsync(dataJson);
        //    }
        //}
        private class ReaderWriter<T>
        {
            protected ReaderWriter()
            {
            }
            private static ReaderWriter<T> readerWriter = null;
            public static ReaderWriter<T> Initialize()
            {
                if (readerWriter == null)
                {
                    readerWriter = new ReaderWriter<T>();
                }
                return readerWriter;
            }
            public async Task<bool> ReadWrite(string path, Func<List<T>, (List<T>, bool)> func)
            {
                Tuple<AutoResetEvent, FileSystemWatcher> tuple = null;
                while (true)
                {
                    try
                    {
                        using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Write))
                        {
                            var data = await ReadAndDesToLString(path, stream);
                            var (needData, needWrite) = func(data);
                            if (needWrite)
                            {
                                await WriteData(path, needData, stream);
                            }
                            //var buffer = Encoding.Default.GetBytes("test");
                            //stream.Write(buffer, 0, buffer.Length);
                            //result = true;
                            return needWrite;
                        }
                    }
                    catch (IOException ex)
                    {
                        // Init only once and only if needed. Prevent against many instantiation in case of multhreaded 
                        // file access concurrency (if file is frequently accessed by someone else). Better memory usage.
                        if (tuple == null)
                        {
                            var autoResetEvent = new AutoResetEvent(true);
                            var fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(path))
                            {
                                EnableRaisingEvents = true
                            };
                            fileSystemWatcher.Filter = Path.GetFileName(path);
                            fileSystemWatcher.Changed +=
                                (o, e) =>
                                {
                                    if (Path.GetFullPath(e.FullPath) == Path.GetFullPath(path))
                                    {
                                        autoResetEvent.Set();
                                        Dispose(tuple);
                                    }
                                };

                            tuple = new Tuple<AutoResetEvent, FileSystemWatcher>(autoResetEvent, fileSystemWatcher);
                        }
                        tuple.Item1.WaitOne(-1);
                    }
                }
                Dispose(tuple);
            }
            private List<T> DesToLString(string dataJson)
            {
                return JsonConvert.DeserializeObject<List<T>>(dataJson);
            }
            private async Task<List<T>> ReadAndDesToLString(string path, FileStream stream)
            {
                var dataJson = await ReadData(path, stream);
                return DesToLString(dataJson);
            }
            private async Task<string> ReadData(string path, FileStream stream)
            {
                //FileSystemWatcher
                var sb = new StringBuilder();
                var count = 256;
                var buffer = new byte[count];
                while (true)
                {
                    var realCount = await stream.ReadAsync(buffer, 0, count);
                    sb.Append(Encoding.Default.GetString(buffer, 0, realCount));
                    if (realCount < count)
                    {
                        break;
                    }
                }
                return sb.ToString();
            }
            private async Task WriteData(string path, object data, FileStream stream)
            {
                stream.SetLength(0);
                var dataJson = JsonConvert.SerializeObject(data);
                var buffer = Encoding.Default.GetBytes(dataJson);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            private void Dispose(Tuple<AutoResetEvent, FileSystemWatcher> tuple)
            {
                if (tuple != null && tuple.Item1 != null) // Dispose of resources now (don't wait the GC).
                {
                    tuple.Item1.Dispose();
                    tuple.Item2.Dispose();
                }
            }
        }
        public async Task<bool> ReadWrite(string path, Func<List<string>, (List<string>, bool)> func)
        {
            var readerWriterStr = ReaderWriter<string>.Initialize();
            Console.WriteLine(readerWriterStr.GetHashCode());
            return await readerWriterStr.ReadWrite(path, func);
        }
        public async Task<bool> ReadWrite(string path, Func<List<UserNicknameAndPasswordAndIPs>, (List<UserNicknameAndPasswordAndIPs>, bool)> func)
        {
            var readerWriterUserNPI = ReaderWriter<UserNicknameAndPasswordAndIPs>.Initialize();
            Console.WriteLine(readerWriterUserNPI.GetHashCode());
            return await readerWriterUserNPI.ReadWrite(path, func);
        }
        public async Task<bool> ReadWrite(string path, Func<List<PersonChat>, (List<PersonChat>, bool)> func)
        {
            var readerPersonChat = ReaderWriter<PersonChat>.Initialize();
            Console.WriteLine(readerPersonChat.GetHashCode());
            return await readerPersonChat.ReadWrite(path, func);
        }
        public async Task<bool> ReadWrite(string path, Func<List<int>, (List<int>, bool)> func)
        {
            var readerPersonChat = ReaderWriter<int>.Initialize();
            Console.WriteLine(readerPersonChat.GetHashCode());
            return await readerPersonChat.ReadWrite(path, func);
        }
        private void SetLastWriteTime(string path)
        {
            File.SetLastWriteTime(path, DateTime.Now);
        }
    }
}
