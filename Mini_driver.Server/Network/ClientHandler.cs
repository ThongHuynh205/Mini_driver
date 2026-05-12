using Mini_driver.Shared.DTO;
using Mini_driver.Shared.Protocol;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Mini_driver.Server.Network;

public class ClientHandler
{
    private readonly TcpClient _client;

    public ClientHandler(TcpClient client)
    {
        _client = client;
    }

    public async Task HandleAsync()
    {
        try
        {
            NetworkStream stream = _client.GetStream();

            while (true)
            {
                byte[] lengthBuffer = new byte[4];

                int readLength =
                    await stream.ReadAsync(lengthBuffer);

                if (readLength == 0)
                    break;

                int packetLength =
                    BitConverter.ToInt32(lengthBuffer, 0);

                byte[] dataBuffer =
                    new byte[packetLength];

                int totalRead = 0;

                while (totalRead < packetLength)
                {
                    int read = await stream.ReadAsync(
                        dataBuffer,
                        totalRead,
                        packetLength - totalRead);

                    totalRead += read;
                }

                string json =
                    Encoding.UTF8.GetString(dataBuffer);

                Packet? packet =
                    JsonSerializer.Deserialize<Packet>(json);

                if (packet == null)
                    continue;

                switch (packet.Type)
                {
                    case PacketType.Login:
                        await HandleLogin(packet, stream);
                        break;

                    case PacketType.UploadFile:
                        await HandleUpload(packet, stream);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task HandleLogin(
        Packet packet,
        NetworkStream stream)
    {
        LoginRequest? request =
            JsonSerializer.Deserialize<LoginRequest>(
                packet.Data);

        LoginResponse response = new LoginResponse();

        if (request?.Username == "admin"
            && request.Password == "123")
        {
            response.Success = true;
            response.Message = "LOGIN SUCCESS";
        }
        else
        {
            response.Success = false;
            response.Message = "INVALID ACCOUNT";
        }

        Packet responsePacket = new Packet
        {
            Type = PacketType.Message,
            Data = JsonSerializer.Serialize(response)
        };

        string json =
            JsonSerializer.Serialize(responsePacket);

        byte[] data =
            Encoding.UTF8.GetBytes(json);

        byte[] length =
            BitConverter.GetBytes(data.Length);

        await stream.WriteAsync(length);

        await stream.WriteAsync(data);
    }

    private async Task HandleUpload(
        Packet packet,
        NetworkStream stream)
    {
        FileInfoDto? fileInfo =
            JsonSerializer.Deserialize<FileInfoDto>(
                packet.Data);

        if (fileInfo == null)
            return;

        Directory.CreateDirectory("Storage");

        string path =
            Path.Combine(
                "Storage",
                fileInfo.FileName);

        using FileStream fs =
            new FileStream(
                path,
                FileMode.Create);

        byte[] buffer = new byte[8192];

        long total = 0;

        while (total < fileInfo.FileSize)
        {
            int read =
                await stream.ReadAsync(buffer);

            if (read == 0)
                break;

            await fs.WriteAsync(buffer, 0, read);

            total += read;

            Console.WriteLine(
                $"Received: {total}/{fileInfo.FileSize}");
        }

        Console.WriteLine("UPLOAD COMPLETE");
    }
}