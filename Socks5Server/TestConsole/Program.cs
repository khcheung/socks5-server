using System;
using System.Net;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Socks5.Server server = new Socks5.Server(IPAddress.Any, 1080)
                .WithAuthentication((username, password) =>
            {
                if (username == "test" && password == "123456")
                {
                    return true;
                }
                return false;
            });
               
            //.RequireTLS();
            server.StartListen();
            Console.WriteLine("Started");
            Console.ReadKey();
        }
    }
}
