using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
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
        private Boolean mRequireTLS = false;
        private Boolean mRequireAuthentication = false;
        private Func<String, String, Boolean> mAuthenticate = null;
        private Byte[] mCert = null;

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

        public Server RequireTLS(Byte[] cert)
        {            
            this.mRequireTLS = true;
            this.mCert = cert;
            return this;
        }

        public Server WithAuthentication(Func<String, String, Boolean> authenticate)
        {
            this.mRequireAuthentication = true;
            this.mAuthenticate = authenticate;
            return this;
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
                        Socks5Handler socks5Handler = null;
                        if (this.mRequireTLS)
                        {
                            var networkStream = tcpClient.GetStream();                            
                            var sslStream = new SslStream(networkStream);
                            sslStream.AuthenticateAsServer(
                                new System.Security.Cryptography.X509Certificates.X509Certificate(this.mCert), 
                                false, 
                                System.Security.Authentication.SslProtocols.Tls, 
                                false);
                            socks5Handler = new Socks5Handler(mConnectionId++, sslStream);
                        }
                        else
                        {
                            socks5Handler = new Socks5Handler(mConnectionId++, tcpClient);
                        }

                        if (mRequireAuthentication)
                        {
                            socks5Handler.WithAuthentication(this.mAuthenticate);
                        }

                        socks5Handler.ConnectionClosed += Socks5Handler_ConnectionClosed;                        
                        this.mConnections.TryAdd(socks5Handler.ConnectionId, socks5Handler);
                        //System.Console.WriteLine($"Connection Count: {this.mConnections.Count}");                    
                        socks5Handler.Start();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex.Message);
                    }
                }
            });
        }

        private void Socks5Handler_ConnectionClosed(object sender, EventArgs e)
        {
            Socks5Handler socks5Handler;
            this.mConnections.TryRemove((sender as Socks5Handler).ConnectionId, out socks5Handler);
            //System.Console.WriteLine($"Connection Count: {this.mConnections.Count}");
        }

        public void StopListen()
        {
            throw new NotImplementedException();
        }
    }
}
