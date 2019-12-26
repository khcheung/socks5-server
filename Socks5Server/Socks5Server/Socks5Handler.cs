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
    public class Socks5Handler
    {
        public Int32 ConnectionId { get; private set; }
        public event EventHandler<EventArgs> ConnectionClosed;
        private TcpClient mTcpClient;
        private Stream mStream;
        private Socks5State mState;

        private Task mTaskReceive;
        private TcpClient mDestClient;
        private Task mTaskDest;
        private Stream mDestStream;

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
                            mDestStream?.Close();
                            //System.Console.WriteLine($"{this.ConnectionId} - CLIENT CLOSE");
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
                                        Byte command = buffer[1];
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
                                        //System.Console.WriteLine($"Connect - {destAddress}:{destPort} - {String.Concat(buffer.Take(count).Select(p => p.ToString("X2")).ToArray())}");
                                        await Connect(destAddress, destPort);

                                    }
                                    break;
                                case Socks5State.Connected:
                                    //System.Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer, 0, count));
                                    await mDestStream.WriteAsync(buffer, 0, count);
                                    //await mDestStream.FlushAsync();
                                    //System.Console.WriteLine($"SEND COUNT - {count}");

                                    break;
                                default:
                                    break;
                            }

                            //mStream.Close();
                            //isClose = true;
                        }
                    }
                }
                catch (Exception)
                {
                    isClose = true;
                    mDestStream.Close();                   
                }
                mStream.Close();
                mTcpClient?.Close();
                //System.Console.WriteLine($"{this.ConnectionId} - CLIENT CLOSE");
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
                this.mDestClient = new TcpClient();
                await this.mDestClient.ConnectAsync(destAddress, destPort);
                this.mDestStream = this.mDestClient.GetStream();
                switch (this.mDestClient.Client.LocalEndPoint.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                    case AddressFamily.InterNetworkV6:
                        var ipEndPoint = this.mDestClient.Client.LocalEndPoint as IPEndPoint;
                        var ipAddress = ipEndPoint.Address.MapToIPv4();
                        //System.Console.WriteLine($"Local EndPoint - {ipEndPoint.AddressFamily} - {ipEndPoint.Address.MapToIPv4().ToString()}");
                        await ReplyConnected(ipAddress.AddressFamily, ipAddress.GetAddressBytes(), ipEndPoint.Port);
                        break;
                }

                this.mState = Socks5State.Connected;

                mTaskDest = Task.Factory.StartNew(async () =>
                {
                    var buffer = new Byte[65535];
                    var count = 0;
                    var isClose = false;
                    try
                    {
                        while (!isClose)
                        {
                            count = await mDestStream.ReadAsync(buffer, 0, buffer.Length);
                            if (count == 0)
                            {
                                isClose = true;
                                mStream.Close();
                               
                            }
                            await this.mStream.WriteAsync(buffer, 0, count);
                            //System.Console.WriteLine($"RECEIVE COUNT - {count}");
                        }
                    } catch (Exception)
                    {                        
                        mStream.Close();
                    }
                    mDestStream.Close();
                    mDestClient.Close();
                    //System.Console.WriteLine($"{this.ConnectionId} - DEST CLOSE");

                });

            }
            catch (Exception) { }
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
    }

    internal enum Socks5State
    {
        Connect,
        Authenticated,
        Connected
    }



    /// <summary>
    /// RFC 1928
    /// Page 3 - METHOD
    /// </summary>
    public enum Method : byte
    {
        NoAuthenticationRequired = 0,
        GSSAPI = 1,
        UsernamePassword = 2
    }

    /// <summary>
    /// RFC 1928
    /// Page 4 - 4. Requests
    /// </summary>
    public enum Command : byte
    {
        Connect = 1,
        Bind = 2,
        UDPAssociate = 3
    }

    /// <summary>
    /// RFC 1928
    /// Page 4 - 4. Requests
    /// Page 6 - 6. Replies
    /// </summary>
    public enum AddressType : byte
    {
        IPV4 = 1,
        DomainName = 3,
        IPV6 = 4
    }

    /// <summary>
    /// RFC 1928
    /// Page 5 - 6. Replies
    /// </summary>
    public enum Reply : byte
    {
        Succeeded = 0,
        ServerFailure = 1,
        ConnectionNotAllowed = 2,
        NetworkUnreachable = 3,
        HostUnreachable = 4,
        ConnectionRefused = 5,
        TTLExpired = 6,
        CommandNotSupported = 7,
        AddressTypeNotSupported = 8
    }
}
