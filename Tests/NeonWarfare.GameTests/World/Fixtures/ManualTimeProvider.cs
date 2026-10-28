namespace NeonWarfare.GameTests.World.Fixtures;

public class ManualTimeProvider(long unixSeconds) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

    public override DateTimeOffset GetUtcNow() => Now;
}
