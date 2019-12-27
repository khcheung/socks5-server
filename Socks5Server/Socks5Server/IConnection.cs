using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Socks5
{
    public interface IConnection
    {
        Task SendAsync(Byte[] buffer);

        Task SendAsync(Byte[] buffer, Int32 offset, Int32 length);
    }
}
