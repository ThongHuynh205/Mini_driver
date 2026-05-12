using System.Net;
using System.Net.Sockets;

namespace Mini_driver.Server.Network;

public class TcpServer
{
    private TcpListener? _listener;

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, 9000);

        _listener.Start();

        Console.WriteLine("SERVER STARTED");

        while (true)
        {
            TcpClient client =
                await _listener.AcceptTcpClientAsync();

            Console.WriteLine("CLIENT CONNECTED");

            _ = Task.Run(async () =>
            {
                ClientHandler handler =
                    new ClientHandler(client);

                await handler.HandleAsync();
            });
        }
    }
}