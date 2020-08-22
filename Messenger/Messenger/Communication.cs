using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    public class Communication
    {
        public Communication(Socket socket)
        {
            this.socket = socket;
        }
        private Socket socket;
        public StringBuilder data;
        public bool EndTask = false;
        private byte[] buffer;
        const int size = 1024;
        public async Task<string> SendMessageListenClient(string message)
        {
            await SendMessage(message);
            return await ListenClient();
        }
        private async Task CheckEndTask(Socket listener)
        {
            if (EndTask == true)
            {
                byte[] byteData = Encoding.ASCII.GetBytes("?/you left the chat");
                await Task.Factory.FromAsync(listener.BeginSend(byteData, 0, byteData.Length, 0, null, null), listener.EndReceive);
                throw new OperationCanceledException();
            }
        }
        public async Task<string> ListenClient()
        {
            var mesLength = await FindMessageLength();

            buffer = new byte[size];
            data = new StringBuilder();
            while (mesLength != data.Length)
            {
                var received = await Task.Factory.FromAsync(socket.BeginReceive(buffer, 0, size, SocketFlags.None, null, null), socket.EndReceive);
                data.Append(Encoding.ASCII.GetString(buffer, 0, received));
            }
            await CheckEndTask(socket);
            return data.ToString();
        }
        public async Task SendMessage(string message)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            var mesLengthByte = CreateFirstMessage(byteData.Length);
            await Task.Factory.FromAsync(socket.BeginSend(mesLengthByte, 0, mesLengthByte.Length, SocketFlags.None, null, null), socket.EndSend);
            await Task.Factory.FromAsync(socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, null, null), socket.EndSend);
        }




        //private byte[] CreateFirstMessage(long byteLength)
        //{
        //    var arrayByteLanght = BitConverter.GetBytes(byteLength);
        //    if (byteLength < 65535)
        //    {
        //        return new byte[3] { arrayByteLanght[0], arrayByteLanght[1], 1 };
        //    }
        //    else if (byteLength < 4294967295)
        //    {
        //        return new byte[6] { arrayByteLanght[0], arrayByteLanght[1], 0, arrayByteLanght[2], arrayByteLanght[3], 1 };
        //    }
        //    else
        //    {
        //        return new byte[10] { arrayByteLanght[0], arrayByteLanght[1], 0, arrayByteLanght[2], arrayByteLanght[3], 0,
        //        arrayByteLanght[4], arrayByteLanght[5], arrayByteLanght[6], arrayByteLanght[7] };
        //    }
        //}
        //private async Task<long> FindMessageLength()
        //{
        //    var byteCount = 3;
        //    var resultBuffer = new List<byte>();
        //    var buffer = new byte[byteCount];
        //    var needAdd = true;
        //    for (int i = 0; i < 2; i++)
        //    {
        //        await Task.Factory.FromAsync(socket.BeginReceive(buffer, 0, byteCount, SocketFlags.None, null, null), socket.EndReceive);
        //        resultBuffer.Add(buffer[0]);
        //        resultBuffer.Add(buffer[1]);
        //        if (buffer[2] == 1)
        //        {
        //            needAdd = false;
        //            break;
        //        }
        //        buffer = new byte[byteCount];
        //    }
        //    if (needAdd)
        //    {
        //        buffer = new byte[4];
        //        await Task.Factory.FromAsync(socket.BeginReceive(buffer, 0, byteCount, SocketFlags.None, null, null), socket.EndReceive);
        //        resultBuffer.AddRange(buffer);
        //    }
        //    if (resultBuffer.Count() == 2)
        //    {
        //        return BitConverter.ToUInt16(resultBuffer.ToArray(), 0);
        //    }
        //    else if (resultBuffer.Count() == 4)
        //    {
        //        return BitConverter.ToUInt32(resultBuffer.ToArray(), 0);
        //    }
        //    else
        //    {
        //        return BitConverter.ToInt64(resultBuffer.ToArray(), 0);
        //    }
        //}
        private byte[] CreateFirstMessage(long byteLength)
        {
            var arrayByteLanght = BitConverter.GetBytes(byteLength);
            if (byteLength < 65535)
            {
                return new byte[3] { 2, arrayByteLanght[0], arrayByteLanght[1] };
            }
            else if (byteLength < 4294967295)
            {
                return new byte[5] { 4, arrayByteLanght[0], arrayByteLanght[1], arrayByteLanght[2], arrayByteLanght[3] };
            }
            else
            {
                return new byte[9] { 8, arrayByteLanght[0], arrayByteLanght[1], arrayByteLanght[2], arrayByteLanght[3],
                arrayByteLanght[4], arrayByteLanght[5], arrayByteLanght[6], arrayByteLanght[7] };
            }
        }
        private async Task<long> FindMessageLength()
        {
            var resultBuffer = new List<byte>();
            var byteCount = 1;
            var buffer = new byte[byteCount];
            await Task.Factory.FromAsync(socket.BeginReceive(buffer, 0, byteCount, SocketFlags.None, null, null), socket.EndReceive);
            return await GetBufferLength(buffer[0]);
        }
        private async Task<long> GetBufferLength(int byteCount)
        {
            var buffer = new byte[byteCount];
            await Task.Factory.FromAsync(socket.BeginReceive(buffer, 0, byteCount, SocketFlags.None, null, null), socket.EndReceive);
            if (byteCount == 2)
            {
                return BitConverter.ToUInt16(buffer, 0);
            }
            else if (byteCount == 4)
            {
                return BitConverter.ToUInt32(buffer, 0);
            }
            else
            {
                return BitConverter.ToInt64(buffer, 0);
            }
        }




        public async Task SendFile(string fileName)
        {
            var lengthByte = CreateFirstMessage(new FileInfo(fileName).Length);
            await Task.Factory.FromAsync(
                socket.BeginSendFile(fileName, lengthByte, null, TransmitFileOptions.UseKernelApc, null, null),
                socket.EndSendFile);
        }

        public async Task ReceiveFile(string path)
        {
            var fileLength = await FindMessageLength();
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                buffer = new byte[size];
                while (stream.Length != fileLength)
                {
                    var received = await Task.Factory.FromAsync(socket.BeginReceive(buffer, 0, size, SocketFlags.None, null, null), 
                        socket.EndReceive);
                    await stream.WriteAsync(buffer, 0, received);
                }
            }
        }
    }
    }
