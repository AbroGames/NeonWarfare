using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// Finds localization keys where they are used — in the code and in the scenes.
/// <br/>
/// Every key-shaped string literal counts, not only the argument of <c>Tr(...)</c>: keys are also
/// passed to <c>[Name(...)]</c> / <c>[Hint(...)]</c> in <c>MenuGameSettings</c>, stored in the
/// dictionary in <c>LoadingScreenTypes</c>, and in the scenes they sit in plain text properties, which
/// Godot substitutes itself (see Docs/Localization.md). Looking only for <c>Tr(...)</c> would report
/// most of the live keys as orphans.
/// </summary>
public static class LocalizationKeys
{
    /// <summary>
    /// SCREAMING_SNAKE_CASE grouped by screen with a double underscore, as Docs/Localization.md
    /// requires: MAIN_MENU__EXIT_BUTTON, MAIN_MENU__RESUME_BUTTON__HOST.
    /// </summary>
    private static readonly Regex KeyRegex =
        new(@"^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*(__[A-Z0-9]+(_[A-Z0-9]+)*)+$", RegexOptions.Compiled);

    /// <summary>Quoted key-shaped text inside a scene file. Scenes are not C#, so this is textual.</summary>
    private static readonly Regex SceneKeyRegex =
        new(@"""(?<key>[A-Z][A-Z0-9]*(_[A-Z0-9]+)*(__[A-Z0-9]+(_[A-Z0-9]+)*)+)""", RegexOptions.Compiled);

    public static bool IsKey(string text) => KeyRegex.IsMatch(text);

    /// <summary>Every key used anywhere in the game — the code and the scenes together.</summary>
    public static IReadOnlyList<LocalizationKeyUsage> Usages() =>
        CSharpFile.LoadAll().SelectMany(InCode)
            .Concat(RepositoryPaths.SceneFiles().Select(SceneFile.Load).SelectMany(InScene))
            .ToList();

    private static IEnumerable<LocalizationKeyUsage> InCode(CSharpFile file) =>
        file.Nodes<LiteralExpressionSyntax>()
            .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
            .Select(literal => (Literal: literal, Text: literal.Token.ValueText))
            .Where(candidate => IsKey(candidate.Text))
            .Select(candidate => new LocalizationKeyUsage(
                candidate.Text, file.RelativePath, file.LineOf(candidate.Literal)));

    private static IEnumerable<LocalizationKeyUsage> InScene(SceneFile scene)
    {
        string[] lines = File.ReadAllText(scene.Path).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in SceneKeyRegex.Matches(lines[i]))
            {
                yield return new LocalizationKeyUsage(
                    match.Groups["key"].Value, scene.RelativePath, i + 1);
            }
        }
    }
}

/// <summary>One place a localization key is used, as a repository-relative path and a line number.</summary>
public sealed record LocalizationKeyUsage(string Key, string RelativePath, int Line)
{
    public string Describe() => $"{RelativePath}:{Line}";
}
