using Socks5.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5
{
    public class LocalConnectionFactory : IConnectionFactory
    {
        public IPeerConnection AcquireConnection(int connectionId, IConnection partner, string destAddress, int destPort)
        {
            return new TcpConnection(connectionId, partner, destAddress, destPort);
        }
    }
}
