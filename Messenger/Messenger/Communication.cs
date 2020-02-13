using System;
using System.Collections.Generic;
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
        public void AnswerClient(Socket listener)
        {
            CheckEndTask(listener);
            buffer = new byte[size];
            data = new StringBuilder();
            do
            {
                listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallback, listener);
                resetReceive.WaitOne();
            } while (listener.Available > 0);
        }
        public void AnswerClient()
        {
            CheckEndTask(listener);
            buffer = new byte[size];
            data = new StringBuilder();
            do
            {
                listener.BeginReceive(buffer, 0, size, SocketFlags.None, ReceiveCallback, listener);
                resetReceive.WaitOne();
            } while (listener.Available > 0);
        }
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
        public void SendMessage(string message, Socket listener)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
            resetSend.WaitOne();
        }
        public void SendMessage(string message)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(message);
            listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
            resetSend.WaitOne();
        }
        private void CheckEndTask(Socket listener)
        {
            if (EndTask == true)
            {
                byte[] byteData = Encoding.ASCII.GetBytes("You are in ban, bye");
                listener.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, listener);
                resetSend.WaitOne();
                throw new OperationCanceledException();
            }
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
    }
}
