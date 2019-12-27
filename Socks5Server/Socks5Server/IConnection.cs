using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Socks5
{
    public interface IConnection
    {
        Task SendAsync(Byte[] buffer);
    }
}
