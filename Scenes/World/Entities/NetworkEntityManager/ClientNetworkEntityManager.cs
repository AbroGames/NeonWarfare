using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ClientNetworkEntityManager : NetworkEntityManager
{
    
    public void AddEntity(INetworkEntity networkEntity, long nid)
    {
        NidToNetworkEntity.Add(nid, networkEntity);
    }
}