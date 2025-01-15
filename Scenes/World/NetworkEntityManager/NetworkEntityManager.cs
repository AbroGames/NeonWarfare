using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public abstract class NetworkEntityManager
{
    protected readonly IDictionary<long, NetworkEntityComponent> NidToNetworkEntity = new Dictionary<long, NetworkEntityComponent>();
    
    public virtual NetworkEntityComponent GetEntityComponent(long nid)
    {
        return NidToNetworkEntity[nid];
    }
    
    public T GetNode<T>(long nid) where T : Node
    {
        return GetEntityComponent(nid).GetParent<T>();
    }
    
    public T GetChild<T>(long nid) where T : Node
    {
        return GetEntityComponent(nid).GetParent<Node>().GetChild<T>();
    }
    
    public bool RemoveEntity(Node node)
    {
        return RemoveEntity(node.GetChild<NetworkEntityComponent>());
    }

    public bool RemoveEntity(NetworkEntityComponent networkEntityComponent)
    {
        return RemoveEntity(networkEntityComponent.Nid);
    }
    
    public bool RemoveEntity(long nid)
    {
        if (!NidToNetworkEntity.ContainsKey(nid)) return false;
        
        NidToNetworkEntity.Remove(nid);
        return true;
    }

    public void Clear()
    {
        NidToNetworkEntity.Clear();
    }
}