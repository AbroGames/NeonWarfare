using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class SafeHud : Hud
{
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	[Export] [NotNull] public Label Fps { get; private set; }
	[Export] [NotNull] public Label SystemInfo { get; private set; }
	
	public ClientSafeWorld ClientSafeWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
		ClientPlayer player = ClientSafeWorld.Player;
		if (player == null) return;

		HpBar.CurrentUpperValue = (float) player.Hp;
		HpBar.CurrentLowerValue = (float) player.Hp;
		HpBar.MaxValue = (float) player.MaxHp;
		HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
		Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";

		PingAnalyzer analyzer = ClientRoot.Instance.Game.PingChecker.PingAnalyzer;
		string pingCurrentInfo = $"Ping: {analyzer.CurrentPingTime} ms";
		string pingSlidingWindowInfo = $"Ping min/avg/max ({PingAnalyzer.MaxTimeOfAnalyticalSlidingWindowForPing/1000}s): {analyzer.MinimumPingTime:N1}/{analyzer.AveragePingTime:N1}/{analyzer.MaximumPingTime:N1} ms";
		string pingPercentileSlidingWindowInfo = $"Ping P50/P90/P99 ({PingAnalyzer.MaxTimeOfAnalyticalSlidingWindowForPing/1000}s): {analyzer.P50PingTime:N1}/{analyzer.P90PingTime:N1}/{analyzer.P99PingTime:N1} ms";
		string packetLossSlidingWindowInfo = $"Packet loss ({PingAnalyzer.ShortTimeOfAnalyticalSlidingWindowForPacketLoss/1000}s/{PingAnalyzer.MidTimeOfAnalyticalSlidingWindowForPacketLoss/1000}s/{PingAnalyzer.MaxTimeOfAnalyticalSlidingWindowForPacketLoss/1000}s): " +
			$"{analyzer.AveragePacketLossInPercentForShortTime:N2}/{analyzer.AveragePacketLossInPercentForMidTime:N2}/{analyzer.AveragePacketLossInPercentForLongTime:N2} %";
		SystemInfo.Text = pingCurrentInfo + "\n" + pingSlidingWindowInfo + "\n" + pingPercentileSlidingWindowInfo + "\n" + packetLossSlidingWindowInfo;
	}
}