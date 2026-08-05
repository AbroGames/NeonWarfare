namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// The shape almost every check in this suite has: two lists of names that must hold the same things —
/// what the code declares and what a document, a settings file or a locale claims. Both directions are
/// always checked, because they fail differently. A thing missing from the document is invisible to
/// whoever reads it; a document naming a thing that no longer exists is worse, since it promises
/// something that is not there.
/// <br/>
/// Only the walk and the exemption list live here. What a failure says stays with the caller: "add a
/// row saying what goes through it" is the part of these tests worth having, and it cannot be
/// generalized.
/// </summary>
public static class CrossCheck
{
    /// <summary>
    /// Reports every item of <paramref name="items"/> that <paramref name="known"/> does not hold.
    /// Order is the caller's — sorted where the source has no meaningful one, as written where it has.
    /// </summary>
    public static void ReportMissing(
        FailureReport report,
        IEnumerable<string> items,
        IReadOnlySet<string> known,
        Func<string, string> describe)
    {
        ReportMissing(report, items, known, [], describe);
    }

    /// <summary>
    /// The same, with the exceptions left out. An exemption is always an explicit array in the calling
    /// test with a comment saying why — never a silent skip, see Docs/Testing.md.
    /// </summary>
    public static void ReportMissing(
        FailureReport report,
        IEnumerable<string> items,
        IReadOnlySet<string> known,
        IReadOnlyCollection<string> exemptions,
        Func<string, string> describe)
    {
        foreach (string item in items)
        {
            if (!known.Contains(item) && !exemptions.Contains(item))
            {
                report.Add(describe(item));
            }
        }
    }
}
