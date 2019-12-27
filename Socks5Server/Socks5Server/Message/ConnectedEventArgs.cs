using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Socks5.Message
{
    public class ConnectedEventArgs : EventArgs
    {
        public AddressFamily AddressFamily { get; set; }
        public Byte[] Address { get; set; }
        public Int32 Port { get; set; }
    }
}
