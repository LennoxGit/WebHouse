using Newtonsoft.Json;

namespace WebHouse_Server.Packets;
//Ein Darenpacket das zwischen dem Server und dem Clieent übertragen wird
public class Packet
{
    public string Data { get; } 
    public PacketDataType DataType { get; }
    public string Sender { get; }
    public string[] Receivers { get; }
    
    public Packet(object data, PacketDataType dataType, string sender, params string[] receivers)
    {
        this.Data = data.GetType() != typeof(string) ? JsonConvert.SerializeObject(data) : (string) data;
        this.DataType = dataType;
        this.Sender = sender;
        this.Receivers = receivers;
    }
}