using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5.Socks5Enum
{
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
}
