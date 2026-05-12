using Microsoft.Win32;
using Mini_driver.Client.Network;
using Mini_driver.Shared.DTO;
using Mini_driver.Shared.Protocol;
using System.Text.Json;

namespace Mini_driver.Client;

public partial class MainWindow : Window
{
    private readonly TcpClientService _client =
        new TcpClientService();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Login_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _client.ConnectAsync();

        LoginRequest request =
            new LoginRequest
            {
                Username = txtUsername.Text,
                Password = txtPassword.Password
            };

        Packet packet =
            new Packet
            {
                Type = PacketType.Login,
                Data = JsonSerializer.Serialize(request)
            };

        await _client.SendPacketAsync(packet);

        Packet response =
            await _client.ReceivePacketAsync();

        LoginResponse? loginResponse =
            JsonSerializer.Deserialize<LoginResponse>(
                response.Data);

        txtStatus.Text =
            loginResponse?.Message;
    }

    private async void Upload_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog =
            new OpenFileDialog();

        if (dialog.ShowDialog() != true)
            return;

        FileInfo file =
            new FileInfo(dialog.FileName);

        FileInfoDto dto =
            new FileInfoDto
            {
                FileName = file.Name,
                FileSize = file.Length
            };

        Packet packet =
            new Packet
            {
                Type = PacketType.UploadFile,
                Data = JsonSerializer.Serialize(dto)
            };

        await _client.SendPacketAsync(packet);

        await _client.SendFileAsync(dialog.FileName);

        txtStatus.Text = "UPLOAD COMPLETE";
    }
}