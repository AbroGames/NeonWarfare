using AbroDraft.Scripts.Utils;
using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.Components.TwoColoredBar;

[GameService]
public class TwoColoredBarService
{
	
	[GameEventListener]
	public void OnTwoColoredBarProcessEvent(TwoColoredBarProcessEvent twoColoredBarProcessEvent)
	{
		TwoColoredBar twoColoredBar = twoColoredBarProcessEvent.TwoColoredBar;
		twoColoredBar.UpperBar.CustomMinimumSize = Vec(twoColoredBar.Width * twoColoredBar.CurrentUpperValuePercent, 0);
		twoColoredBar.LowerBar.CustomMinimumSize = Vec(twoColoredBar.Width * twoColoredBar.CurrentLowerValuePercent, 0);
	}
}