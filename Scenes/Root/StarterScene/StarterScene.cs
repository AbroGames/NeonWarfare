using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.CmdArgs;

namespace NeonWarfare.Scenes.Root.StarterScene;

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
