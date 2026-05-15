using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Mini_driver.Server.Networking
{
    public class ServerListener
    {
        private TcpListener _listener;
        private readonly int _port;
        private bool _isRunning;

        public ServerListener(int port)
        {
            _port = port;
        }

        public void Start()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;
                Console.WriteLine("[SERVER STARTED] Cổng: " + _port);

                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ClientHandler handler = new ClientHandler(client);
                    Thread clientThread = new Thread(new ThreadStart(handler.Handle));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SERVER ERROR] " + ex.Message);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_listener != null)
            {
                _listener.Stop();
            }
        }
    }
}