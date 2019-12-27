using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5.Socks5Enum
{
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
