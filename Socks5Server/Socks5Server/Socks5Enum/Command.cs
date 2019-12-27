using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5.Socks5Enum
{

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
}
