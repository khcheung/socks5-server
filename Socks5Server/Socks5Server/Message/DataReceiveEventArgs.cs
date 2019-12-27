using System;
using System.Collections.Generic;
using System.Text;

namespace Socks5.Message
{
    internal class DataReceiveEventArgs : EventArgs
    {
        public Byte[] Data { get; set; }
    }
}
