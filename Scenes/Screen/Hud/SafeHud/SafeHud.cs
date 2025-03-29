using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ClientGame.Ping;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.Components.TwoColoredBar;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scenes.World.SafeWorld.ClientSafeWorld;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.Screen.SafeHud;

public partial class SafeHud : Hud
{
	[Export] [NotNull] public ReadyPlayersListContainer ReadyPlayersList { get; private set; }
	[Export] [NotNull] public Button GoToBattleButton { get; private set; }
	[Export] [NotNull] public Button SelfResurrectButton { get; private set; }
	public ClientSafeWorld ClientSafeWorld { get; set; }

	private AutoCooldown _resurrectCooldown = new AutoCooldown(0.5);
	
	public override void _Ready()
	{
		base._Ready();
		NotNullChecker.CheckProperties(this);
		
		_resurrectCooldown.ActionWhenReady += () =>
		{
			SelfResurrectButton.Visible = ClientRoot.Instance.Game.World.Player.IsDead;
		};

		SelfResurrectButton.Pressed += () =>
		{
			SelfResurrectButton.Visible = false;
			Network.SendToServer(new ServerGame.CS_ClientRequestedSelfResurrection());
		};
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		_resurrectCooldown.Update(delta);
	}
	
	public override ClientWorld GetCurrentWorld()
	{
		return ClientSafeWorld;
	}
}
