namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// One thing that went wrong in one process: the line that says so and the lines that explain it — a
/// stack trace, the exception text, the last output of a process that missed a milestone.
/// </summary>
public sealed record Problem(string Process, string Headline, IReadOnlyList<string> Details);
