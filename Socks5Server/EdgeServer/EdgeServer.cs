using Socks5;
using Socks5.Interface;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Edge
{
    public class EdgeServer : IConnectionFactory
    {
        private IPAddress mListenIp = null;
        private Int32 mPort = 0;
        private TcpListener mTcpListener = null;
        private Task mListenTask = null;

        public EdgeServer(IPAddress listenIp, Int32 port)
        {
            this.mListenIp = listenIp;
            this.mPort = port;
        }

        public IPeerConnection AcquireConnection(int connectionId, IConnection partner, string destAddress, int destPort)
        {            
            return new TcpConnection(connectionId, partner, destAddress, destPort);
        }

        public void StartListen()
        {
            if (mTcpListener == null)
            {
                mTcpListener = new TcpListener(mListenIp, mPort);
                mTcpListener.Start();
                ListenAndAccept();
            }
            else
            {
                throw new Exception("TcpListener Not Null");
            }
        }

        private void ListenAndAccept()
        {
            mListenTask = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var tcpClient = await mTcpListener.AcceptTcpClientAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex.Message);
                    }
                }
            });
        }
    }
}
