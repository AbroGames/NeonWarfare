using System.Linq;
using Godot;
using KludgeBox;
using NeoVector;

namespace NeonWarfare;

public partial class StarterScene : Node2D
{
	
	[Export] [NotNull] public PackedScene ClientRoot { get; private set; }
	[Export] [NotNull] public PackedScene ServerRoot { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
        CallDeferred(nameof(StartGame));
	}

	private void StartGame()
	{
		PackedScene rootScene = OS.GetCmdlineArgs().Contains(ServerParams.ServerFlag) ? ServerRoot : ClientRoot;
		GetTree().ChangeSceneToPacked(rootScene);
	}
}