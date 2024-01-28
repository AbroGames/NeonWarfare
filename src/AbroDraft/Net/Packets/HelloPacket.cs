using Godot;

namespace AbroDraft.Net.Packets;

public partial class HelloPacket : AbstractPacket
{
    public string Message = "HAI";
}