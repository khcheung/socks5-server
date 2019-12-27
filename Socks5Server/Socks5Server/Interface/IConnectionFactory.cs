using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5.Interface
{
    public interface IConnectionFactory
    {
        IPeerConnection AcquireConnection(Int32 connectionId, IConnection partner, String destAddress, Int32 destPort);
    }
}
