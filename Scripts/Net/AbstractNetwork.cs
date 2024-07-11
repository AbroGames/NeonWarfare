using Godot;
using KludgeBox;

namespace NeonWarfare.Net;

public partial class AbstractNetwork : Node
{
    
    public MultiplayerApi Api { get; private set; }
    public ENetConnection ENet { get; private set; }
    public ENetMultiplayerPeer Peer { get; private set; }
    
    public virtual void Init()
    {
        Api = Root.Instance.GetTree().GetMultiplayer();
        ENet = new ENetConnection();
        Peer = new ENetMultiplayerPeer();
        Log.Info("AbstractNetwork init complete");
    }
}