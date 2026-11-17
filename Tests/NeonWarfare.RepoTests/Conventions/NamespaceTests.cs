using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Conventions;

/// <summary>
/// "Namespaces mirror the path to the file" from Docs/Code-style.md. C# does not care, so a file moved
/// between folders keeps its old namespace and nothing complains until someone goes looking for the
/// type where the folder says it should be.
/// </summary>
public class NamespaceTests
{
    /// <summary>
    /// Files that must declare no namespace at all. Global usings have to be at file level, so
    /// GlobalUsings.cs cannot have one — see Docs/Repository-structure.md.
    /// </summary>
    private static readonly string[] WithoutNamespace = ["Src/NeonWarfare/Scripts/GlobalUsings.cs"];

    [Theory]
    [MemberData(nameof(FileSources.Sources), MemberType = typeof(FileSources))]
    public void Namespace_MatchesFilePath(string relativePath)
    {
        CSharpFile file = CSharpFile.Load(RepositoryPaths.Absolute(relativePath));
        IReadOnlyList<BaseNamespaceDeclarationSyntax> declared =
            file.Nodes<BaseNamespaceDeclarationSyntax>().ToList();

        // Asserted rather than skipped: an exempted file that grows a namespace has stopped being the
        // exception the list was written for.
        if (WithoutNamespace.Contains(relativePath))
        {
            Assert.True(
                declared.Count == 0,
                $"{relativePath}: this file is listed as namespace-free but declares " +
                $"{string.Join(", ", declared.Select(declaration => declaration.Name.ToString()))}");
            return;
        }

        Assert.True(
            declared.Count == 1,
            $"{relativePath}: expected exactly one namespace declaration, found {declared.Count}");

        string expected = ExpectedNamespace(relativePath);
        Assert.True(
            string.Equals(declared[0].Name.ToString(), expected, StringComparison.Ordinal),
            $"{relativePath}: namespace must be '{expected}', found '{declared[0].Name}'");
    }

    [Fact]
    public void WithoutNamespace_ListsExistingFiles()
    {
        IReadOnlySet<string> sources = RepositoryPaths.SourceFiles()
            .Select(RepositoryPaths.Relative)
            .ToHashSet(StringComparer.Ordinal);

        CrossCheck.AssertExemptionsExist(
            nameof(WithoutNamespace), WithoutNamespace, sources.Contains, "no such source file");
    }

    /// <summary>
    /// Derived from the path rather than hardcoded, so a new folder needs no change here. The path is
    /// taken relative to Src/ with no prefix of its own — NeonWarfare.csproj has an empty RootNamespace:
    /// Src/NeonWarfare/Scenes/Worlds/Features/Chat/ChatSimulation.cs →
    /// NeonWarfare.Scenes.Worlds.Features.Chat,
    /// Src/GodotBox/Godot/Nodes/Background.cs → GodotBox.Godot.Nodes.
    /// </summary>
    private static string ExpectedNamespace(string relativePath)
    {
        string directory = Path.GetDirectoryName(RepositoryPaths.Absolute(relativePath))!;
        return Path.GetRelativePath(RepositoryPaths.SrcDirectory, directory)
            .Replace(Path.DirectorySeparatorChar, '.');
    }
}
