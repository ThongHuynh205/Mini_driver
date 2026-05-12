using Mini_driver.Shared.Protocol;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Mini_driver.Client.Network;

public class TcpClientService
{
    private readonly TcpClient _client =
        new TcpClient();

    private NetworkStream? _stream;

    public async Task ConnectAsync()
    {
        if (_client.Connected)
            return;

        await _client.ConnectAsync(
            "127.0.0.1",
            9000);

        _stream = _client.GetStream();
    }

    public async Task SendPacketAsync(Packet packet)
    {
        if (_stream == null)
            return;

        string json =
            JsonSerializer.Serialize(packet);

        byte[] data =
            Encoding.UTF8.GetBytes(json);

        byte[] length =
            BitConverter.GetBytes(data.Length);

        await _stream.WriteAsync(length);

        await _stream.WriteAsync(data);
    }

    public async Task<Packet> ReceivePacketAsync()
    {
        if (_stream == null)
            throw new Exception("NO STREAM");

        byte[] lengthBuffer = new byte[4];

        await _stream.ReadAsync(lengthBuffer);

        int length =
            BitConverter.ToInt32(lengthBuffer, 0);

        byte[] dataBuffer = new byte[length];

        int totalRead = 0;

        while (totalRead < length)
        {
            int read =
                await _stream.ReadAsync(
                    dataBuffer,
                    totalRead,
                    length - totalRead);

            totalRead += read;
        }

        string json =
            Encoding.UTF8.GetString(dataBuffer);

        return JsonSerializer.Deserialize<Packet>(
            json)!;
    }

    public async Task SendFileAsync(string path)
    {
        if (_stream == null)
            return;

        using FileStream fs =
            new FileStream(path, FileMode.Open);

        byte[] buffer = new byte[8192];

        int read;

        while ((read = await fs.ReadAsync(buffer)) > 0)
        {
            await _stream.WriteAsync(
                buffer,
                0,
                read);
        }
    }
}