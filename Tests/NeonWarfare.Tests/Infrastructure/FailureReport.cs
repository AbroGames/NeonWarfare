using System.Text;
using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// Collects every violation found by a check and fails once with the whole list.
/// Failing on the first violation would mean fixing documentation one round-trip at a time.
/// </summary>
public sealed class FailureReport
{
    private readonly string _rule;
    private readonly List<string> _failures = [];

    public FailureReport(string rule)
    {
        _rule = rule;
    }

    public bool IsEmpty => _failures.Count == 0;

    public void Add(string failure) => _failures.Add(failure);

    public void AssertEmpty() => Assert.True(IsEmpty, ToString());

    public override string ToString()
    {
        StringBuilder message = new();
        message.AppendLine($"{_rule}: {_failures.Count} violation(s)");
        foreach (string failure in _failures)
        {
            message.AppendLine($"  - {failure}");
        }

        return message.ToString();
    }
}
