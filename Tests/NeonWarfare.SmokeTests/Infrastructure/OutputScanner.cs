using System.Text.RegularExpressions;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Decides whether a launched game printed anything bad.
///
/// The exit code is useless as a signal: ExceptionHandlerService catches unhandled exceptions, logs
/// them and lets the process run on, and the only GetTree().Quit() in the game passes no code. So the
/// output is all there is.
/// </summary>
public static partial class OutputScanner
{
    /// <summary>
    /// A Serilog line from the game. The level is rendered by KludgeBox's RichGodotSink as the full
    /// level name padded left to the width of "Information" — the {Level:u3} in the template is
    /// ignored by that renderer, so this matches "(      Error)" and not "(ERR)".
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
    /// GD.PrintRich emits BBCode, which the engine turns into ANSI escapes when stdout is a terminal.
    /// Redirected output should be plain, but stripping is cheap insurance against a leading escape
    /// sequence pushing the timestamp off column zero and hiding an error.
    /// </summary>
    [GeneratedRegex(@"\x1B\[[0-9;]*m")]
    private static partial Regex AnsiEscapeRegex();

    /// <summary>
    /// Every problem found in one process's output, each already prefixed with the process name.
    /// An engine error drags its stack trace along: "ERROR: NullReferenceException" on its own says
    /// nothing about where it came from.
    /// </summary>
    public static IReadOnlyList<string> Scan(GameProcess process)
    {
        string[] lines = process.Output.Select(line => AnsiEscapeRegex().Replace(line, string.Empty)).ToArray();
        List<string> problems = [];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (SerilogProblemRegex().IsMatch(line))
            {
                problems.Add($"[{process.Name}] {line}");
                continue;
            }

            if (!EngineProblemRegex().IsMatch(line)) continue;

            problems.Add($"[{process.Name}] {line}");
            foreach (string traceLine in TakeTrace(lines, i))
            {
                problems.Add($"[{process.Name}]     {traceLine.TrimEnd()}");
            }
        }

        return problems;
    }

    /// <summary>
    /// The indented continuation of an engine error, up to the first blank or unindented line.
    /// </summary>
    private static IEnumerable<string> TakeTrace(string[] lines, int problemIndex)
    {
        for (int i = problemIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0 || !char.IsWhiteSpace(line[0])) yield break;

            yield return line;
        }
    }
}
