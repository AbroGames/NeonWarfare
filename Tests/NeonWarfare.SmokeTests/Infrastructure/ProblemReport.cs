namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Turns the problems of a run into the text of the failure.
///
/// An exception thrown on every physics frame repeats thousands of times, and printing each repeat made a
/// report of tens of thousands of lines that an IDE or a CI log cuts off — while the cause was already in
/// its first block. So identical problems are printed once with a count, and the report is capped.
/// </summary>
public static class ProblemReport
{
    /// <summary>
    /// Roughly how many lines the report may take. A block is never cut in half, and the first one is
    /// printed whatever its size.
    /// </summary>
    private const int MaxLines = 200;

    /// <summary>
    /// Null when there are no problems; otherwise the report, each distinct problem in the order it
    /// first appeared.
    /// </summary>
    public static string? Render(IReadOnlyList<Problem> problems)
    {
        if (problems.Count == 0) return null;

        List<(Problem First, int Count)> distinct = Collapse(problems);

        List<string> lines = [$"The game run has {problems.Count} problem(s), {distinct.Count} distinct:"];
        int shown = 0;
        foreach ((Problem problem, int count) in distinct)
        {
            List<string> block = Block(problem, count);
            if (shown > 0 && lines.Count + block.Count > MaxLines) break;

            lines.AddRange(block);
            shown++;
        }

        if (shown < distinct.Count)
        {
            lines.Add($"...and {distinct.Count - shown} more distinct problem(s), not shown.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Groups problems that differ only in the Serilog timestamp. Everything else — the process, the
    /// headline, every detail line — must match, so two different stack traces behind the same message
    /// stay apart.
    /// </summary>
    private static List<(Problem First, int Count)> Collapse(IReadOnlyList<Problem> problems)
    {
        List<(Problem First, int Count)> distinct = [];
        Dictionary<string, int> indexBySignature = new(StringComparer.Ordinal);

        foreach (Problem problem in problems)
        {
            string signature = string.Join('\n',
                [problem.Process, OutputScanner.WithoutTimestamp(problem.Headline), .. problem.Details]);

            if (indexBySignature.TryGetValue(signature, out int index))
            {
                distinct[index] = (distinct[index].First, distinct[index].Count + 1);
                continue;
            }

            indexBySignature[signature] = distinct.Count;
            distinct.Add((problem, 1));
        }

        return distinct;
    }

    private static List<string> Block(Problem problem, int count)
    {
        string repeats = count > 1 ? $"({count} times) " : "";
        List<string> block = [$"[{problem.Process}] {repeats}{problem.Headline}"];
        block.AddRange(problem.Details.Select(detail => $"[{problem.Process}]     {detail}"));
        return block;
    }
}
