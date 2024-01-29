namespace KludgeBox.Scheduling;

public static class ClockEvents
{
	public enum TickStage
	{
		/// <summary>
		/// This is start stage of tick
		/// </summary>
		Start,

		/// <summary>
		/// This is end of tick
		/// </summary>
		End
	}

	/// <summary>
	/// Fired on the every clock tick
	/// </summary>
	public class TickEvent
	{
		/// <summary>
		/// Current stage of the tick
		/// </summary>
		public TickStage Stage;

		/// <summary>
		/// Time in seconds since previous tick
		/// </summary>
		public double Delta { get; }

		/// <summary>
		/// Current tick number
		/// </summary>
		public long Tick { get; }

		/// <summary>
		/// Name of the clock that fired the event
		/// </summary>
		public string ClockName { get; }


		public TickEvent(TickStage stage, double delta, long tick, string name = null)
		{
			Stage = stage;
			Delta = delta;
			Tick = tick;
			ClockName = name;
		}
	}
}