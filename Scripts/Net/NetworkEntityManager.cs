using System.Collections.Generic;
using Godot;

namespace NeoVector;

public class NetworkEntityManager
{
    private readonly IDictionary<Node2D, long> _nodeToNid = new Dictionary<Node2D, long>(ReferenceEqualityComparer.Instance);
    private readonly IDictionary<long, Node2D> _nidToNode = new Dictionary<long, Node2D>();
    private long _nextNid = 0; 
    //TODO поделить диапазон между игроками. Например, по 1ккк каждому новому игроку (если игрок переподключился, то ему дают новый диапазон).
    //TODO а как менеджить объекты игрока, когда он отключится? На пули можно забить, а турели и остальное? Можно делать менеджемент на клиенте исключительно для снарядов. А остальное на сервере.
    //TODO а как в террарии? Может просто на стороне сервера создавать пули и не ебаться? Может в пулю добавить id создавшего игрока и reqNid, в ответ передавать Nid и reqNid, чтобы игрок понял, что это его пуля и менеджил ее. При этом nid-ы будут генерироваться только на сервере, получается.
    //TODO а что если надо уничтожить снаряд раньше чем игроку пришел nid от сервера? Или уничтожение переложить полностью на плечи сервера?
    //TODO Вынести _nextNid на серверную сторону, на клиенте только два маппинга
    
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

    public long RemoveEntity(Node2D node)
    {
        if (!_nodeToNid.ContainsKey(node)) return -1;
        
        long nid = _nodeToNid[node];
        _nodeToNid.Remove(node);
        _nidToNode.Remove(nid);
        return nid;
    }
    
    public Node2D RemoveEntity(long nid)
    {
        if (!_nidToNode.ContainsKey(nid)) return null;
        
        Node2D node = _nidToNode[nid];
        _nidToNode.Remove(nid);
        _nodeToNid.Remove(node);
        return node;
    }

    public void Clear()
    {
        _nodeToNid.Clear();
        _nidToNode.Clear();
    }
}