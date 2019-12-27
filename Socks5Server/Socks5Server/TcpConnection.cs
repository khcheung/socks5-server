using Socks5.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Socks5
{
    internal class TcpConnection : IConnection
    {
        private Int32 mConnectionId = 0;
        private TcpClient mTcpClient = null;
        private Stream mStream = null;
        private String mDestAddress = null;
        private Int32 mDestPort = 0;
        private Task mReceiveTask = null;
        private Boolean mIsClosed = false;
        private IConnection mPartner = null;

        public event EventHandler<ConnectedEventArgs> Connected;        
        public event EventHandler Closed;


        private CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        public TcpConnection(Int32 connectionId, IConnection partner, String destAddress, Int32 destPort)
        {
            this.mConnectionId = connectionId;
            this.mPartner = partner;
            this.mDestAddress = destAddress;
            this.mDestPort = destPort;
        }

        public async Task Connect()
        {
            if (mTcpClient == null)
            {
                try
                {
                    this.mTcpClient = new TcpClient();
                    await this.mTcpClient.ConnectAsync(mDestAddress, mDestPort);
                    this.mStream = this.mTcpClient.GetStream();

                    switch (this.mTcpClient.Client.LocalEndPoint.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                        case AddressFamily.InterNetworkV6:
                            var ipEndPoint = this.mTcpClient.Client.LocalEndPoint as IPEndPoint;
                            var ipAddress = ipEndPoint.Address.MapToIPv4();

                            Connected?.Invoke(this, new ConnectedEventArgs()
                            {
                                AddressFamily = ipAddress.AddressFamily,
                                Address = ipAddress.GetAddressBytes(),
                                Port = ipEndPoint.Port
                            });

                            this.Receive();

                            break;
                    }


                }
                catch (Exception) { }
            }
        }

        public async Task SendAsync(Byte[] buffer)
        {
            await this.mStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private void Receive()
        {
            mReceiveTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    var buffer = new Byte[65535];
                    var count = 0;
                    var isClose = false;
                    while (!isClose)
                    {
                        count = await mStream.ReadAsync(buffer, 0, buffer.Length);
                        if (count == 0)
                        {
                            isClose = true;
                        }
                        else
                        {
                            await mPartner.SendAsync(buffer.Take(count).ToArray());
                        }
                    }
                }
                catch (Exception) { }
                this.Closed?.Invoke(this, new EventArgs());
                this.Close();
            });
        }

        public void Close()
        {
            if (!mIsClosed)
            {
                mIsClosed = true;
                mCancellationTokenSource.Cancel();
                this.mStream?.Close();
                this.mStream?.Dispose();
                this.mStream = null;
                this.mTcpClient?.Close();
                this.mTcpClient?.Dispose();
                this.mTcpClient = null;
            }
        }
    }
}
