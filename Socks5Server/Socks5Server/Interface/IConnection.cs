using Socks5.Message;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Socks5.Interface
{
    public interface IConnection
    {
        Task SendAsync(Byte[] buffer);

        Task SendAsync(Byte[] buffer, Int32 offset, Int32 length);

        void Close();
    }

    public interface IPeerConnection : IConnection
    {
        event EventHandler<ConnectedEventArgs> Connected;
        event EventHandler Closed;

        Task Connect();
    }
}
