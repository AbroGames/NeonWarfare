using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Docs/Repository-structure.md draws the folder tree with a line of explanation each. It is the first
/// thing read before a task and the last thing anyone remembers to update: a renamed folder leaves a
/// tree that describes a repository that no longer exists.
/// <br/>
/// Only one direction is checked — that everything drawn is really there. The tree is deliberately
/// partial (Scenes/Entity is one line, not a subtree), so demanding the reverse would be demanding a
/// different document.
/// </summary>
public class RepositoryStructureDocTests
{
    private const string DocumentName = "Repository-structure.md";

    /// <summary>The characters the tree is drawn with; the entry name starts after the last of them.</summary>
    private const string TreeCharacters = "│├└─ ";

    /// <summary>One level of nesting is four columns wide, connector included.</summary>
    private const int IndentWidth = 4;

    [Fact]
    public void DrawnPaths_ExistInTheRepository()
    {
        MarkdownDocument document = MarkdownDocument.LoadDoc(DocumentName);

        FailureReport report = new($"Docs/{DocumentName} draws paths that do not exist");
        List<string> parents = [];
        int drawn = 0;

        for (int i = 0; i < document.Lines.Length; i++)
        {
            // Only the fenced block holds the tree; the fence lines themselves have no entry on them.
            if (!document.IsFenced[i])
            {
                continue;
            }

            string line = document.Lines[i];
            int start = line.TakeWhile(TreeCharacters.Contains).Count();
            if (start >= line.Length || line.StartsWith("```", StringComparison.Ordinal))
            {
                continue;
            }

            string entry = line[start..].Split(' ')[0];
            int depth = start / IndentWidth;

            if (depth > parents.Count)
            {
                report.Add($"line {i + 1}: '{entry}' is indented {start} columns, which skips a level — " +
                           $"one level is {IndentWidth} columns");
                continue;
            }

            parents.RemoveRange(depth, parents.Count - depth);
            parents.Add(entry.TrimEnd('/'));

            string relativePath = string.Join('/', parents);
            string absolutePath = RepositoryPaths.Absolute(relativePath);
            drawn++;

            bool exists = entry.EndsWith('/')
                ? Directory.Exists(absolutePath)
                : File.Exists(absolutePath);

            if (!exists)
            {
                report.Add($"line {i + 1}: {relativePath}");
            }
        }

        // A tree that stopped being recognized as a tree would make this test pass while checking
        // nothing at all.
        Assert.True(drawn > 20, $"expected the tree to hold more entries than {drawn}, the parser missed it");
        report.AssertEmpty();
    }
}
