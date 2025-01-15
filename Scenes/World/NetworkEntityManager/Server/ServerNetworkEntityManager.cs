using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ServerNetworkEntityManager : NetworkEntityManager
{
    
    private long _nextNid = 0; 
    
    public void AddEntity(Node node)
    {
        ServerNetworkEntityComponent networkEntityComponent = new ServerNetworkEntityComponent(_nextNid++);
        node.AddChild(networkEntityComponent);
        NidToNetworkEntity.Add(networkEntityComponent.Nid, networkEntityComponent);
    }
    
    public override ServerNetworkEntityComponent GetEntityComponent(long nid)
    {
        return (ServerNetworkEntityComponent) base.GetEntityComponent(nid);
    }
}