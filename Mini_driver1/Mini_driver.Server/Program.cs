using System;
using Mini_driver.Server.Networking;

namespace Mini_driver.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Thiết lập bảng mã hiển thị tiếng Việt trên Console không bị lỗi font
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== MINI DRIVER SERVER SYSTEM ===");

            int port = 8888;
            ServerListener server = new ServerListener(port);
            server.Start();
        }
    }
}