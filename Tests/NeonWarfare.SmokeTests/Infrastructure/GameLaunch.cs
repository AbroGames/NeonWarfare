namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// One process of a scenario: its name in the report, the game flags, and the milestones — fragments
/// of log lines that prove it got where it was sent. Silence is not success: a client still waiting on
/// ENet prints nothing bad, and neither does one the server quietly dropped.
/// </summary>
public sealed record GameLaunch(string Name, IReadOnlyList<string> Arguments, IReadOnlyList<string> Milestones);
