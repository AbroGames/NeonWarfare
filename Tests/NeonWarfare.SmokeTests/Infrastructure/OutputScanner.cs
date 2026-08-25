using System.Text.RegularExpressions;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Decides whether a launched game printed anything bad.
///
/// While the game runs, the exit code is useless as a signal: ExceptionHandlerService catches unhandled
/// exceptions, logs them and lets the process run on. So the output is all there is. The exit code only
/// means something after SIGTERM, and GameProcess.Stop checks it there.
/// </summary>
public static partial class OutputScanner
{
    /// <summary>
    /// Any Serilog line from the game, whatever its level: "|09:40:22.851| (...".
    /// </summary>
    [GeneratedRegex(@"^\|[\d:.]+\| ")]
    private static partial Regex SerilogLineRegex();

    /// <summary>
    /// A Serilog line at a level that fails the test. The level is rendered by KludgeBox's RichGodotSink
    /// as the full level name padded left to the width of "Information" — the {Level:u3} in the template
    /// is ignored by that renderer, so this matches "(      Error)" and not "(ERR)".
    /// </summary>
    [GeneratedRegex(@"^\|[\d:.]+\| \( *(Warning|Error|Fatal)\)")]
    private static partial Regex SerilogProblemRegex();

    /// <summary>
    /// The engine's own channel, printed at column zero. Unhandled managed exceptions arrive this way,
    /// as "ERROR:" followed by a stack trace.
    /// </summary>
    [GeneratedRegex(@"^(ERROR|WARNING|SCRIPT ERROR|USER ERROR|USER WARNING):")]
    private static partial Regex EngineProblemRegex();

    /// <summary>
    /// Every problem found in one process's output, each already prefixed with the process name.
    /// A problem drags its details along: "ERROR: NullReferenceException" or "(Error) Failed to load"
    /// on its own says nothing about where it came from.
    /// </summary>
    public static IReadOnlyList<string> Scan(GameProcess process)
    {
        IReadOnlyList<string> lines = process.Output;
        List<string> problems = [];

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];

            IEnumerable<string> details;
            if (SerilogProblemRegex().IsMatch(line))
            {
                details = TakeFollowing(lines, i, IsSerilogContinuation);
            }
            else if (EngineProblemRegex().IsMatch(line))
            {
                details = TakeFollowing(lines, i, IsEngineTrace);
            }
            else
            {
                continue;
            }

            problems.Add($"[{process.Name}] {line}");
            foreach (string detail in details)
            {
                problems.Add($"[{process.Name}]     {detail.TrimEnd()}");
            }
        }

        return problems;
    }

    /// <summary>
    /// RichGodotSink prints the message and then the exception with a separate GD.Print, so the
    /// exception text starts at column zero ("System.NullReferenceException: ...") and is followed by
    /// its "   at" frames. It belongs to the problem up to the next log line of either kind.
    /// </summary>
    private static bool IsSerilogContinuation(string line) =>
        line.Length > 0 && !SerilogLineRegex().IsMatch(line) && !EngineProblemRegex().IsMatch(line);

    /// <summary>
    /// The indented continuation of an engine error: "   at: ...", "   C# backtrace ...".
    /// </summary>
    private static bool IsEngineTrace(string line) => line.Length > 0 && char.IsWhiteSpace(line[0]);

    private static IEnumerable<string> TakeFollowing(
        IReadOnlyList<string> lines, int problemIndex, Func<string, bool> belongs)
    {
        for (int i = problemIndex + 1; i < lines.Count && belongs(lines[i]); i++)
        {
            yield return lines[i];
        }
    }
}
