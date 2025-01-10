using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public abstract class NetworkEntityManager
{
    protected readonly IDictionary<long, INetworkEntity> NidToNetworkEntity = new Dictionary<long, INetworkEntity>();
    
    public INetworkEntity GetNetworkEntity(long nid)
    {
        return NidToNetworkEntity[nid];
    }
    
    public T GetNode<T>(long nid) where T : Node
    {
        return GetNetworkEntity(nid) as T;
    }

    public bool RemoveNetworkEntity(INetworkEntity networkEntity)
    {
        return RemoveNetworkEntity(networkEntity.Nid);
    }
    
    public bool RemoveNetworkEntity(long nid)
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