using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ServerEnemy : ServerCharacter
{
    [Export] [NotNull] public RayCast2D RayCast { get; private set; }
}