using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// A comment refers to the documentation as <c>Docs/&lt;File&gt;.md#&lt;anchor&gt;</c> instead of retelling it,
/// see Docs/Code-style.md#comments. Such a reference has to resolve like a link inside <c>Docs/</c> does: the
/// file must exist, and the anchor must be one of its headings.
/// </summary>
public class CodeDocsReferenceTests
{
    // A path inside a longer one (Tests/Docs/…) is not a reference to Docs/.
    private static readonly Regex ReferenceRegex = new(
        @"(?<![\w/])Docs/(?<file>[\w.-]+\.md)(?:#(?<anchor>[\p{L}\p{N}_-]+))?",
        RegexOptions.Compiled);

    private static readonly HashSet<SyntaxKind> CommentKinds =
    [
        SyntaxKind.SingleLineCommentTrivia,
        SyntaxKind.MultiLineCommentTrivia,
        SyntaxKind.SingleLineDocumentationCommentTrivia,
        SyntaxKind.MultiLineDocumentationCommentTrivia,
    ];

    [Theory]
    [MemberData(nameof(FileSources.CSharpFiles), MemberType = typeof(FileSources))]
    public void CommentReferences_PointToExistingDocsAndAnchors(string relativePath)
    {
        CSharpFile file = CSharpFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: comment references to missing docs or sections");

        // Comment trivia only: a string literal that happens to hold a path is not a reference.
        foreach (SyntaxTrivia comment in file.Root.DescendantTrivia(descendIntoTrivia: false))
        {
            if (!CommentKinds.Contains(comment.Kind()))
            {
                continue;
            }

            foreach (Match match in ReferenceRegex.Matches(comment.ToFullString()))
            {
                // The line of the reference itself: a doc comment spans several lines.
                int line = comment.SyntaxTree!.GetLineSpan(new TextSpan(comment.FullSpan.Start + match.Index, 0))
                    .StartLinePosition.Line + 1;
                string where = $"{relativePath}:{line} → {match.Value}";
                string target = RepositoryPaths.Doc(match.Groups["file"].Value);
                if (!File.Exists(target))
                {
                    report.Add($"{where}: {RepositoryPaths.Relative(target)} does not exist");
                    continue;
                }

                string anchor = match.Groups["anchor"].Value;
                if (anchor.Length > 0 && !MarkdownDocument.Load(target).HeadingSlugs.Contains(anchor))
                {
                    report.Add($"{where}: no heading with anchor '{anchor}'");
                }
            }
        }

        report.AssertEmpty();
    }
}
