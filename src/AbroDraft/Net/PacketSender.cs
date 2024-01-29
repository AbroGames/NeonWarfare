namespace AbroDraft.Net;

public record PacketSender(long Id)
{
    public bool IsServer => Id == 1;
    public bool IsClient => !IsServer;
    public bool IsLocalClient => Id == 0;
    
    
    public static implicit operator PacketSender(long id) => new PacketSender(id);
    public static implicit operator long(PacketSender sender) => sender.Id;
}