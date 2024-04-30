using Godot;

namespace NeoVector;

public partial class Network2 : Node
{
    
    public MultiplayerApi Api { get; private set; }
    public ENetMultiplayerPeer Peer { get; private set; }
    
    public override void _Ready()
    {
        Api = Root.Instance.GetTree().GetMultiplayer();
        Peer = new ENetMultiplayerPeer();
    }
}