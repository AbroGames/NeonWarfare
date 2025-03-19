using Godot;
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
	[Export] [NotNull] public Label SystemInfo { get; private set; }
	
	public ClientSafeWorld ClientSafeWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
		PingAnalyzer analyzer = ClientRoot.Instance.Game.PingChecker.PingAnalyzer;
		string pingCurrentInfo = $"Ping: {analyzer.CurrentPingTime} ms";
		string pingSlidingWindowInfo = $"Ping min/avg/max ({PingAnalyzer.MaxTimeOfAnalyticalSlidingWindowForPing/1000}s): {analyzer.MinimumPingTime:N1}/{analyzer.AveragePingTime:N1}/{analyzer.MaximumPingTime:N1} ms";
		string pingPercentileSlidingWindowInfo = $"Ping P50/P90/P99 ({PingAnalyzer.MaxTimeOfAnalyticalSlidingWindowForPing/1000}s): {analyzer.P50PingTime:N1}/{analyzer.P90PingTime:N1}/{analyzer.P99PingTime:N1} ms";
		string packetLossSlidingWindowInfo = $"Packet loss ({PingAnalyzer.ShortTimeOfAnalyticalSlidingWindowForPacketLoss/1000}s/{PingAnalyzer.MidTimeOfAnalyticalSlidingWindowForPacketLoss/1000}s/{PingAnalyzer.MaxTimeOfAnalyticalSlidingWindowForPacketLoss/1000}s): " +
			$"{analyzer.AveragePacketLossInPercentForShortTime:N2}/{analyzer.AveragePacketLossInPercentForMidTime:N2}/{analyzer.AveragePacketLossInPercentForLongTime:N2} %";
		SystemInfo.Text = pingCurrentInfo + "\n" + pingSlidingWindowInfo + "\n" + pingPercentileSlidingWindowInfo + "\n" + packetLossSlidingWindowInfo;
		
		base._Process(delta);
	}
	
	public override ClientWorld GetCurrentWorld()
	{
		return ClientSafeWorld;
	}
}
