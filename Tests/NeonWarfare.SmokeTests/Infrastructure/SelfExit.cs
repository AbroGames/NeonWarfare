namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// One process of a scenario quitting on its own once something outside it happens — a child server whose
/// parent client died. <paramref name="Exiter"/> is a <see cref="GameLaunch.Name"/>; <paramref name="Cause"/> makes
/// the outside thing happen. The exiter's <see cref="GameLaunch.ExitMilestones"/> prove it quit for that reason.
/// </summary>
public sealed record SelfExit(string Exiter, Action Cause);
