using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5.Socks5Enum
{
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
}
