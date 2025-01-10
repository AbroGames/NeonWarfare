using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ServerNetworkEntityManager : NetworkEntityManager
{
    
    private long _nextNid = 0; 
    
    public long AddEntity(INetworkEntity networkEntity)
    {
        long nextNid = _nextNid++;
        NidToNetworkEntity.Add(nextNid, networkEntity);

        return nextNid;
    }
}