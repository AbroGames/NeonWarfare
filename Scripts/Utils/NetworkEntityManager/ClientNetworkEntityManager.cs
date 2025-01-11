using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ClientNetworkEntityManager : NetworkEntityManager
{
    
    public void AddEntity(NetworkEntityComponent networkEntityComponent)
    {
        NidToNetworkEntity.Add(networkEntityComponent.Nid, networkEntityComponent);
    }
    
    public void AddEntity(Node node)
    {
        AddEntity(node.GetChild<NetworkEntityComponent>());
    }
}