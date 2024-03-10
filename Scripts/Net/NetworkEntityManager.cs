using System.Collections.Generic;

namespace NeoVector;

public class NetworkEntityManager
{
    private readonly IDictionary<object, long> _entityToNid = new Dictionary<object, long>(ReferenceEqualityComparer.Instance);
    private readonly IDictionary<long, object> _nidToEntity = new Dictionary<long, object>();
    private long _nextNid = 0;

    public void AddEntity(object @object)
    {
        long nextNid = _nextNid++;
        _entityToNid.Add(@object, nextNid);
        _nidToEntity.Add(nextNid, @object);
    }

    public void RemoveEntity(object @object)
    {
        long nid = _entityToNid[@object];
        _entityToNid.Remove(@object);
        _nidToEntity.Remove(nid);
    }
    
    public void RemoveEntity(long nid)
    {
        object @object = _nidToEntity[nid];
        _nidToEntity.Remove(nid);
        _entityToNid.Remove(@object);
    }
}