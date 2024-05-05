using Godot;

namespace NeonWarfare.Net;

public partial class AbstractNetwork : Node
{
    
    public MultiplayerApi Api { get; private set; }
    public ENetMultiplayerPeer Peer { get; private set; }
    
    public void Init()
    {
        Api = Root.Instance.GetTree().GetMultiplayer();
        Peer = new ENetMultiplayerPeer();
    }
}