using Socks5.Message;
using Socks5.Socks5Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Socks5
{
    public class Socks5Handler : IConnection
    {
        
        public event EventHandler<EventArgs> ConnectionClosed;

        public Int32 ConnectionId { get; private set; }
        private TcpClient mTcpClient;
        private Stream mStream;
        private Socks5State mState;
        private Boolean mIsClosed = false;
        private Task mTaskReceive = null;

        private TcpConnection mTcpConnection;        

        public Socks5Handler(Int32 connectionId, TcpClient tcpClient)
        {
            this.ConnectionId = connectionId;
            this.mTcpClient = tcpClient;
            this.mStream = tcpClient.GetStream();
        }

        public Socks5Handler(Int32 connectionId, Stream stream)
        {
            this.ConnectionId = connectionId;
            this.mStream = stream;
        }

        public void Start()
        {
            var buffer = new Byte[65535];
            var count = 0;
            var isClose = false;
            this.mTaskReceive = Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!isClose)
                    {
                        count = await mStream.ReadAsync(buffer, 0, buffer.Length);
                        if (count == 0)
                        {
                            isClose = true;
                        }
                        else
                        {
                            //System.Console.WriteLine(String.Concat(buffer.Take(count).Select(p => p.ToString("X2")).ToArray()));                            
                            switch (this.mState)
                            {
                                case Socks5State.Connect:
                                    {
                                        Byte version = buffer[0];
                                        Byte noOfMethods = buffer[1];
                                        Byte method = buffer[2];

                                        if (version != 5)
                                        {
                                            System.Console.WriteLine("Invalid Socks Version");
                                            mStream.Close();
                                            isClose = true;
                                        }

                                        if (method == 0)
                                        {
                                            await ReplyConnect();
                                            this.mState = Socks5State.Authenticated;
                                        }

                                    }
                                    break;
                                case Socks5State.Authenticated:
                                    {
                                        Int32 position = 0;
                                        Byte version = buffer[0];
                                        Command command = (Command)buffer[1];
                                        Byte reserved = buffer[2];
                                        AddressType addressType = (AddressType)buffer[3];
                                        IPAddress address = null;
                                        String destAddress = "";

                                        if (version != 5)
                                        {
                                            System.Console.WriteLine("Invalid Socks Version");
                                            mStream.Close();
                                            isClose = true;
                                        }
                                        else
                                        {

                                            switch (addressType)
                                            {
                                                case AddressType.IPV4:
                                                    address = new IPAddress(buffer.Skip(4).Take(4).ToArray());
                                                    destAddress = address.ToString();
                                                    position = 8;
                                                    break;
                                                case AddressType.IPV6:
                                                    address = new IPAddress(buffer.Skip(4).Take(16).ToArray());
                                                    destAddress = address.ToString();
                                                    position = 8;
                                                    break;
                                                case AddressType.DomainName:
                                                    byte length = buffer[4];
                                                    destAddress = System.Text.Encoding.ASCII.GetString(buffer.Skip(5).Take((Int32)length).ToArray());
                                                    position = 5 + length;
                                                    break;
                                            }

                                            Int32 destPort = (buffer[position] << 8) + buffer[position + 1];

                                            switch(command)
                                            {
                                                case Command.Connect:
                                                    await Connect(destAddress, destPort);
                                                    break;
                                                case Command.Bind:
                                                    System.Console.WriteLine("Command BIND - Not Implemented");
                                                    isClose = true;
                                                    break;
                                                case Command.UDPAssociate:
                                                    System.Console.WriteLine("Command UDP Associate - Not Implemented");
                                                    isClose = true;
                                                    break;
                                            }
                                            
                                        }

                                    }
                                    break;
                                case Socks5State.Connected:
                                    await this.mTcpConnection.SendAsync(buffer, 0, count);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (Exception) { }
                mTcpConnection.Close();
                this.Close();
                ConnectionClosed?.Invoke(this, new EventArgs());
            });
        }

        private async Task ReplyConnect()
        {
            await mStream.WriteAsync(new byte[] { 5, 0 }, 0, 2);
        }

        private async Task Connect(String destAddress, Int32 destPort)
        {
            try
            {
                this.mTcpConnection = new TcpConnection(this.ConnectionId, this, destAddress, destPort);
                mTcpConnection.Connected += TcpConnection_Connected;                
                mTcpConnection.Closed += TcpConnection_Closed;
                await mTcpConnection.Connect();
            }
            catch (Exception) { }
        }

        private void Close()
        {
            if (!mIsClosed)
            {
                mIsClosed = true;
                mStream?.Close();
                mStream?.Dispose();
                mStream = null;
                mTcpClient?.Close();
                mTcpClient?.Dispose();
                mTcpClient = null;
            }
        }

        private void TcpConnection_Closed(object sender, EventArgs e)
        {
            this.Close();
        } 

        private void TcpConnection_Connected(object sender, ConnectedEventArgs e)
        {
            this.mState = Socks5State.Connected;
            Task.Run(async () =>
            {
                await this.ReplyConnected(e.AddressFamily, e.Address, e.Port);
            });
        }

        private async Task ReplyConnected(AddressFamily addressFamily, Byte[] address, Int32 port)
        {
            List<Byte> reply = new List<byte>();
            reply.AddRange(new byte[] { 5, 0, 0 });
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    reply.Add((Byte)AddressType.IPV4);
                    break;
                case AddressFamily.InterNetworkV6:
                    reply.Add((Byte)AddressType.IPV6);
                    break;
            }
            reply.AddRange(address);
            reply.Add((byte)((port & 0xff00) >> 8));
            reply.Add((byte)(port & 0xff));
            await mStream.WriteAsync(reply.ToArray(), 0, reply.Count);
            //System.Console.WriteLine($"Reply - {String.Concat(reply.Select(p => p.ToString("X2")).ToArray())}");
        }

        public async Task SendAsync(byte[] buffer)
        {
            await this.SendAsync(buffer, 0, buffer.Length);
        }

        public async Task SendAsync(byte[] buffer, int offset, int length)
        {
            await this.mStream.WriteAsync(buffer, offset, length);
        }
    }
}
