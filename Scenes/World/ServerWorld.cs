using Godot;
using KludgeBox;
using NeonWarfare.NetOld;

namespace NeonWarfare;

public abstract partial class ServerWorld : Node2D
{

    public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
    public Player Player;
}