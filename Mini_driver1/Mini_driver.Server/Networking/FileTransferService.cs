#nullable disable
using System;
using System.IO;
using System.Net.Sockets;

namespace Mini_driver.Server.Networking
{
    public class FileTransferService
    {
        public static bool ReceiveFile(NetworkStream stream, string savePath, long fileSize)
        {
            try
            {
                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while (totalBytesRead < fileSize && (bytesRead = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - totalBytesRead))) > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FILE TRANSFER ERROR] " + ex.Message);
                return false;
            }
        }

        public static bool SendFile(NetworkStream stream, string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }
                    stream.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FILE TRANSFER ERROR] " + ex.Message);
                return false;
            }
        }
    }
}