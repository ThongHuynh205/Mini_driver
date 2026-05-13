namespace Mini_driver.Shared.Protocol;

public class Packet
{
    public PacketType Type { get; set; }

    public string Data { get; set; } = string.Empty;
}