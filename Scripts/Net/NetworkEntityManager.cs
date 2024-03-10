using System.Collections.Generic;
using Godot;

namespace NeoVector;

public class NetworkEntityManager
{
    private readonly IDictionary<Node2D, long> _nodeToNid = new Dictionary<Node2D, long>(ReferenceEqualityComparer.Instance);
    private readonly IDictionary<long, Node2D> _nidToNode = new Dictionary<long, Node2D>();
    private long _nextNid = 0;

    public long AddEntity(Node2D node)
    {
        long nextNid = _nextNid++;
        _nodeToNid.Add(node, nextNid);
        _nidToNode.Add(nextNid, node);

        return nextNid;
    }
    
    public void AddEntity(Node2D node, long nid)
    {
        _nodeToNid.Add(node, nid);
        _nidToNode.Add(nid, node);
    }

    public long GetNid(Node2D node)
    {
        return _nodeToNid[node];
    }
    
    public Node2D GetNode(long nid)
    {
        return _nidToNode[nid];
    }
    
    public T GetNode<T>(long nid) where T : Node2D
    {
        return _nidToNode[nid] as T;
    }

    public void RemoveEntity(Node2D node)
    {
        if (!_nodeToNid.ContainsKey(node)) return;
        
        long nid = _nodeToNid[node];
        _nodeToNid.Remove(node);
        _nidToNode.Remove(nid);
    }
    
    public void RemoveEntity(long nid)
    {
        if (!_nidToNode.ContainsKey(nid)) return;
        
        Node2D node = _nidToNode[nid];
        _nidToNode.Remove(nid);
        _nodeToNid.Remove(node);
    }
}