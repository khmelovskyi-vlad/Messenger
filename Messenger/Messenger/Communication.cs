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
    class Communication
    {
        public Communication(Socket listener)
        {
            this.listener = listener;
        }
        private Socket listener;
        public StringBuilder data;
        public bool EndTask = false;
        private byte[] buffer;
        const int size = 256;
        AutoResetEvent resetSend = new AutoResetEvent(false);
        AutoResetEvent resetReceive = new AutoResetEvent(false);
        public void SendMessageAndAnswerClient(string message)
        {
            SendMessage(message);
            AnswerClient();
        }
        public void AnswerClient(Socket listener)
        {
            buffer = new byte[size];
            data = new StringBuilder();
            do
            {
                listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallback, listener);
                resetReceive.WaitOne();
            } while (listener.Available > 0);
            CheckEndTask(listener);
        }
        public void AnswerClient()
        {
            buffer = new byte[size];
            data = new StringBuilder();
            do
            {
                listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallback, listener);
                resetReceive.WaitOne();
            } while (listener.Available > 0);
            CheckEndTask(listener);
        }
        //public void AnswerClientt()
        //{
        //    buffer = new byte[size];
        //    data = new StringBuilder();
        //    listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallbackk, listener);
        //    CheckEndTask(listener);
        //}
        //public string TakeMessage()
        //{
        //    resetReceive.WaitOne();
        //    return data.ToString();
        //}
        //private void ReceiveCallbackk(IAsyncResult AR)
        //{
        //    Socket current = (Socket)AR.AsyncState;
        //    var received = current.EndReceive(AR);
        //    if (received > 0)
        //    {
        //        data.Append(Encoding.ASCII.GetString(buffer, 0, received));
        //    }
        //    if (current.Available > 0)
        //    {
        //        current.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallbackk, current);
        //    }
        //    else
        //    {
        //        resetReceive.Set();
        //    }
        //}
        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                resetReceive.Set();
                return;
            }
            data.Append(Encoding.ASCII.GetString(buffer, 0, received));
            resetReceive.Set();
        }
        private void CheckEndTask(Socket listener)
        {
            if (EndTask == true)
            {
                byte[] byteData = Encoding.ASCII.GetBytes("?/you left the chat");
                listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
                resetSend.WaitOne();
                throw new OperationCanceledException();
            }
        }
        public void SendMessage(string message, Socket listener)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
            //resetSend.WaitOne();
        }
        public void SendMessage(string message)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
            resetSend.WaitOne();
        }
        private void SendCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            try
            {
                current.EndSend(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Can`t send message");
            }
            resetSend.Set();
        }
        public void SendFile(string fileName)
        {
            var length = new FileInfo(fileName).Length;
            var lengthByte = BitConverter.GetBytes(length);
            listener.BeginSendFile(fileName, lengthByte, null, TransmitFileOptions.UseKernelApc, SendFileCallback, listener);
            resetSend.WaitOne();
        }
        private void SendFileCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            try
            {
                current.EndSendFile(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Can`t send file");
            }
            resetSend.Set();
        }
        public async Task ReceiveFile(string path)
        {
            buffer = new byte[8];
            listener.BeginReceive(buffer, 0, 8, SocketFlags.None, ReceiveFileCallback, listener);
            resetReceive.WaitOne();
            var fileLength = BitConverter.ToInt64(buffer, 0);
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                buffer = new byte[size];
                do
                {
                    listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveFileCallback, listener);
                    resetReceive.WaitOne();
                    await stream.WriteAsync(buffer, 0, countReceivedBytes);
                } while (stream.Length != fileLength);
            }
        }
        public async Task ReceiveFile2(string path)
        {
            buffer = new byte[8];
            var bufferSize = listener.Receive(buffer);
            var fileLength = BitConverter.ToInt64(buffer, 0);
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                buffer = new byte[size];
                var endSend = 0;
                do
                {
                    bufferSize = listener.Receive(buffer);
                    endSend = endSend + bufferSize;
                    await stream.WriteAsync(buffer, 0, bufferSize);
                } while (endSend != fileLength);
            }
        }
        private int countReceivedBytes = 0;
        private void ReceiveFileCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            try
            {
                countReceivedBytes = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                resetReceive.Set();
                return;
            }
            resetReceive.Set();
        }
        public void ReceiveFile3(string path)
        {
            buffer = new byte[8];
            var bufferSize = listener.Receive(buffer);
            var fileLength = BitConverter.ToInt64(buffer, 0);
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                buffer = new byte[size];
                do
                {
                    bufferSize = listener.Receive(buffer);
                    stream.Write(buffer, 0, bufferSize);
                } while (stream.Length != fileLength);
            }
        }
        public void ReceiveFile4(string path)
        {
            buffer = new byte[8];
            var bufferSize = listener.Receive(buffer);
            var fileLength = BitConverter.ToInt64(buffer, 0);
            using (var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                StateObject stateObject = new StateObject { socket = listener, stream = stream, fileLength = fileLength };
                listener.BeginReceive(stateObject.buffer, 0, StateObject.bufSize, SocketFlags.None, ReceiveFileCallbackk, stateObject);
                resetReceive.WaitOne();
            }
        }
        public class StateObject
        {
            public long fileLength;
            public Socket socket = null;
            public const int bufSize = 1024;
            public byte[] buffer = new byte[bufSize];
            public FileStream stream;
        }
        private void ReceiveFileCallbackk(IAsyncResult AR)
        {
            var stateObject = (StateObject)AR.AsyncState;
            var received = stateObject.socket.EndReceive(AR);
            if (received > 0)
            {
                stateObject.stream.Write(stateObject.buffer, 0, received);
            }
            if (stateObject.fileLength != stateObject.stream.Length)
            {
                stateObject.socket.BeginReceive(stateObject.buffer, 0, StateObject.bufSize, SocketFlags.None, ReceiveFileCallbackk, stateObject);
            }
            else
            {
                resetReceive.Set();
            }
        }
    }
}
