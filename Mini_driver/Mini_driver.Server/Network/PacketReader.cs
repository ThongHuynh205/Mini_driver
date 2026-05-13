using System.Net.Sockets;

namespace Mini_driver.Server.Network;

public static class PacketReader
{
    public static async Task<byte[]> ReadBytesAsync(
        NetworkStream stream,
        int length)
    {
        byte[] buffer = new byte[length];

        int totalRead = 0;

        while (totalRead < length)
        {
            int read = await stream.ReadAsync(
                buffer,
                totalRead,
                length - totalRead);

            if (read == 0)
                break;

            totalRead += read;
        }

        return buffer;
    }
}