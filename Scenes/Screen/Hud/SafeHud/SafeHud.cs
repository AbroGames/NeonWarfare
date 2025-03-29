using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ClientGame.Ping;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.Components.TwoColoredBar;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scenes.World.SafeWorld.ClientSafeWorld;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.SafeHud;

public partial class SafeHud : Hud
{
	[Export] [NotNull] public ReadyPlayersListContainer ReadyPlayersList { get; private set; }
	[Export] [NotNull] public Button GoToBattleButton { get; private set; }
	public ClientSafeWorld ClientSafeWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		GoToBattleButton.Visible = ClientRoot.Instance.Game.PlayerProfile.IsAdmin;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}
	
	public override ClientWorld GetCurrentWorld()
	{
		return ClientSafeWorld;
	}
}
