using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ServerNetworkEntityManager : NetworkEntityManager
{
    
    //static для того, чтобы у следующего NetworkEntityManager не повторялись id от предыдущего.
    //Иначе могут прийти пакеты от предыдущего NetworkEntityManager и примениться к новому.
    private static long _nextNid = 0; 
    
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