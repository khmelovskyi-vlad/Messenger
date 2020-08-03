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
    static class FileMaster
    {
        //public FileMaster(string usersPath)
        //{
        //    this.UsersPath = usersPath;
        //}
        //private string UsersPath { get; }


        static public void DeleterFolder(string path)
        {
            Directory.Delete(path, true);
        }
        static public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
        static public string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }
        static public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }
        static public string[] ReadUsersPaths()
        {
            return Directory.GetFiles(@"D:\temp\messenger\Users");
        }
        static public async Task<List<T>> ReadAndDeserialize<T>(string path)
        {
            return JsonConvert.DeserializeObject<List<T>>(await ReadData(path));
        }
        static public async Task<string> ReadData(string path)
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
        static private void Dispose(Tuple<AutoResetEvent, FileSystemWatcher> tuple)
        {
            if (tuple != null && tuple.Item1 != null) // Dispose of resources now (don't wait the GC).
            {
                tuple.Item1.Dispose();
                tuple.Item2.Dispose();
            }
        }
        static private async Task<string> ReadData(FileStream stream)
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
        private class UpdaterFile<T>
        {
            protected UpdaterFile()
            {
            }
            private static UpdaterFile<T> readerWriter = null;
            public static UpdaterFile<T> Initialize()
            {
                if (readerWriter == null)
                {
                    readerWriter = new UpdaterFile<T>();
                }
                return readerWriter;
            }
            public async Task<bool> UpdateFile(string path, Func<List<T>, (List<T>, bool)> func)
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
                var datas = await ReadAndDesToLString(path, stream);
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
        //public async Task<bool> ReadWrite(string path, Func<List<string>, (List<string>, bool)> func)
        //{
        //    var readerWriterStr = ReaderWriter<string>.Initialize();
        //    Console.WriteLine(readerWriterStr.GetHashCode());
        //    return await readerWriterStr.ReadWrite(path, func);
        //}
        //public async Task<bool> ReadWrite(string path, Func<List<UserNicknameAndPasswordAndIPs>, (List<UserNicknameAndPasswordAndIPs>, bool)> func)
        //{
        //    var readerWriterUserNPI = ReaderWriter<UserNicknameAndPasswordAndIPs>.Initialize();
        //    Console.WriteLine(readerWriterUserNPI.GetHashCode());
        //    return await readerWriterUserNPI.ReadWrite(path, func);
        //}
        //public async Task<bool> ReadWrite(string path, Func<List<PersonChat>, (List<PersonChat>, bool)> func)
        //{
        //    var readerPersonChat = ReaderWriter<PersonChat>.Initialize();
        //    Console.WriteLine(readerPersonChat.GetHashCode());
        //    return await readerPersonChat.ReadWrite(path, func);
        //}
        //public async Task<bool> ReadWrite(string path, Func<List<int>, (List<int>, bool)> func)
        //{
        //    var readerPersonChat = ReaderWriter<int>.Initialize();
        //    Console.WriteLine(readerPersonChat.GetHashCode());
        //    return await readerPersonChat.ReadWrite(path, func);
        //}
        static public async Task<bool> UpdateFile<T>(string path, Func<List<T>, (List<T>, bool)> func)
        {
            var readerPersonChat = UpdaterFile<T>.Initialize();
            Console.WriteLine(readerPersonChat.GetHashCode());
            return await readerPersonChat.UpdateFile(path, func);
        }
        static public Func<List<T>, (List<T>, bool)> AddData<T>(T data)
        {
            return (datas =>
            {
                if (datas == null)
                {
                    datas = new List<T>();
                }
                datas.Add(data);
                return (datas, true);
            });
        }
        static public Func<List<T>, (List<T>, bool)> AddSomeData<T>(List<T> someData)
        {
            return (datas =>
            {
                if (datas == null)
                {
                    datas = new List<T>();
                }
                datas.AddRange(someData);
                return (datas, true);
            });
        }
        static private void SetLastWriteTime(string path)
        {
            File.SetLastWriteTime(path, DateTime.Now);
        }
    }
}
