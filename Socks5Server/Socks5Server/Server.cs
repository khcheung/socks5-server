using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Socks5
{
    public class Server
    {
        private IPAddress mListenIp;
        private Int32 mPort;
        private TcpListener mTcpListener;
        private ConcurrentDictionary<Int32, Socks5Handler> mConnections;
        private Int32 mConnectionId = 0;

        /// <summary>
        /// Socks5 Server on Address and Port
        /// </summary>
        /// <param name="listenIp"></param>
        /// <param name="port"></param>
        public Server(IPAddress listenIp, Int32 port)
        {
            this.mListenIp = listenIp;
            this.mPort = port;
        }

        public void StartListen()
        {
            if (mTcpListener == null)
            {
                mTcpListener = new TcpListener(mListenIp, mPort);
                mConnections = new ConcurrentDictionary<int, Socks5Handler>();
                mTcpListener.Start();
                ListenAndAccept();
            }
            else
            {
                // ToDo: Check Status, Stop, Start
                throw new Exception("TcpListener Not Null");
            }
        }

        private void ListenAndAccept()
        {
            var listenTask = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var tcpClient = await mTcpListener.AcceptTcpClientAsync();
                        Socks5Handler socks5Handler = new Socks5Handler(mConnectionId++, tcpClient);
                        socks5Handler.ConnectionClosed += Socks5Handler_ConnectionClosed;                        
                        this.mConnections.TryAdd(socks5Handler.ConnectionId, socks5Handler);
                        System.Console.WriteLine($"Connection Count: {this.mConnections.Count}");                    
                        socks5Handler.Start();
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        private void Socks5Handler_ConnectionClosed(object sender, EventArgs e)
        {
            Socks5Handler socks5Handler;
            this.mConnections.TryRemove((sender as Socks5Handler).ConnectionId, out socks5Handler);
            System.Console.WriteLine($"Connection Count: {this.mConnections.Count}");
        }

        public void StopListen()
        {
            throw new NotImplementedException();
        }
    }
}
