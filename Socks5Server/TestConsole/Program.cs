using System;
using System.Net;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var cert = @"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
xOXdqZUZXRqUbQQIlVKwePW8g3MCAggA";

            Socks5.Server server = new Socks5.Server(IPAddress.Any, 1080)
                .WithAuthentication((username, password) =>
                {
                    if (username == "test" && password == "123456")
                    {
                        return true;
                    }
                    return false;
                })
                .RequireTLS(Convert.FromBase64String(cert));              
            
            server.StartListen();
            Console.WriteLine("Started");
            Console.ReadKey();
        }
    }
}
