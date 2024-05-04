using Godot;

namespace NeonWarfare;

public partial class StarterScene : Node2D
{
	[Export] public PackedScene Scene { get; private set; }
	public override void _Ready()
	{
		GetTree().ChangeSceneToPacked(Scene);
	}
}