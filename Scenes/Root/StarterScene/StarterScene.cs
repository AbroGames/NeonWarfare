using Godot;
using KludgeBox;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare;

public partial class StarterScene : Node
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
		PackedScene rootScene = CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag) ? ServerRoot : ClientRoot;
		GetTree().ChangeSceneToPacked(rootScene);
	}
}