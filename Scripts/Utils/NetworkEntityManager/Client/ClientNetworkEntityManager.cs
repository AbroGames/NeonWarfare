using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

public class ClientNetworkEntityManager : NetworkEntityManager
{
    
    public void AddEntity(Node node, long nid)
    {
        ClientNetworkEntityComponent networkEntityComponent = new ClientNetworkEntityComponent(nid);
        node.AddChild(networkEntityComponent);
        NidToNetworkEntity.Add(networkEntityComponent.Nid, networkEntityComponent);
    }

    public override ClientNetworkEntityComponent GetEntityComponent(long nid)
    {
        return (ClientNetworkEntityComponent) base.GetEntityComponent(nid);
    }
}
