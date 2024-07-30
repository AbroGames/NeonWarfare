using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientEffectsPackedScenesContainer : Node
{
	[Export] [NotNull] public PackedScene DeathFx { get; private set; }
	[Export] [NotNull] public PackedScene DebrisFx { get; private set; }
	[Export] [NotNull] public PackedScene BulletHitFx { get; private set; }
	[Export] [NotNull] public PackedScene SpawnFx { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}
}