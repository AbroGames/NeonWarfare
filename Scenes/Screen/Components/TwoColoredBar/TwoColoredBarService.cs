[GameService]
public class TwoColoredBarService
{
	public TwoColoredBarService()
	{
		Root.Instance.EventBus.Subscribe<TwoColoredBarProcessEvent>(OnTwoColoredBarProcessEvent);
	}
    
	public void OnTwoColoredBarProcessEvent(TwoColoredBarProcessEvent twoColoredBarProcessEvent)
	{
		UpdateProgressBar(twoColoredBarProcessEvent.TwoColoredBar);
	}

	public void UpdateProgressBar(TwoColoredBar twoColoredBar)
	{
		twoColoredBar.UpperBar.CustomMinimumSize = Vec(twoColoredBar.Width * twoColoredBar.CurrentUpperValuePercent, 0);
		twoColoredBar.LowerBar.CustomMinimumSize = Vec(twoColoredBar.Width * twoColoredBar.CurrentLowerValuePercent, 0);
	}
}
