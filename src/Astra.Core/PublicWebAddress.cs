using System.Net;
using System.Net.Sockets;

namespace AstraOS.Core;

public static class PublicWebAddress
{
    public static Uri Parse(string text)
    {
        if(text.Length>2000||!Uri.TryCreate(text,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Port!=443||uri.UserInfo.Length!=0||uri.HostNameType==UriHostNameType.Unknown)throw new ArgumentException("Usa un indirizzo HTTPS pubblico, senza credenziali o porte personalizzate.");
        string host=uri.DnsSafeHost.TrimEnd('.').ToLowerInvariant();
        if((!host.Contains('.')&&!IPAddress.TryParse(uri.Host.Trim('[',']'),out _))||host is "localhost"||host.EndsWith(".localhost")||host.EndsWith(".local")||host.EndsWith(".internal")||host.EndsWith(".home")||host.EndsWith(".lan"))throw new ArgumentException("Indirizzo locale non consentito nella ricerca web.");
        if(IPAddress.TryParse(uri.Host.Trim('[',']'),out var address)&&!IsPublic(address))throw new ArgumentException("Indirizzo di rete non pubblico.");
        return uri;
    }
    public static bool IsPublic(IPAddress address)
    {
        if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();
        if(IPAddress.IsLoopback(address))return false;
        var b=address.GetAddressBytes();
        if(address.AddressFamily==AddressFamily.InterNetwork)return !(b[0] is 0 or 10 or 127 or >=224||b[0]==169&&b[1]==254||b[0]==172&&b[1] is >=16 and <=31||b[0]==192&&(b[1]==168||b[1]==0)||b[0]==100&&b[1] is >=64 and <=127||b[0]==198&&b[1] is 18 or 19||b[0]==198&&b[1]==51&&b[2]==100||b[0]==203&&b[1]==0&&b[2]==113);
        return address.AddressFamily==AddressFamily.InterNetworkV6&&(b[0]&0xE0)==0x20&&!(b[0]==0x20&&b[1]==1&&b[2]==0x0D&&b[3]==0xB8);
    }
}
