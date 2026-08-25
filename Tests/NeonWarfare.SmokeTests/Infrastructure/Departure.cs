namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// One process of a scenario quitting while the rest keep running — a player closing the game in the
/// middle of a session. <paramref name="Leaver"/> and <paramref name="Witness"/> are
/// <see cref="GameLaunch.Name"/>s; <paramref name="Milestone"/> is the log line fragment that proves the
/// witness noticed. Without it the departure could go unseen, and "nothing bad was printed" would pass.
/// </summary>
public sealed record Departure(string Leaver, string Witness, string Milestone);
