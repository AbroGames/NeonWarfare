using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ServerWorld : Node2D
{

    public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
    public Player Player;
}