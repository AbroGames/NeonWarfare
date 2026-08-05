using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Conventions;

/// <summary>
/// "Namespaces mirror the path to the file" from Docs/Code-style.md. C# does not care, so a file moved
/// between folders keeps its old namespace and nothing complains until someone goes looking for the
/// type where the folder says it should be.
/// </summary>
public class NamespaceTests
{
    private const string RootNamespace = "NeonWarfare";

    /// <summary>
    /// Files that must declare no namespace at all. Global usings have to be at file level, so
    /// GlobalUsings.cs cannot have one — see Docs/Repository-structure.md.
    /// </summary>
    private static readonly string[] WithoutNamespace = ["Scripts/GlobalUsings.cs"];

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

    /// <summary>
    /// Derived from the path rather than hardcoded, so a new folder needs no change here:
    /// Scenes/World/Service/Chat/WorldChatService.cs → NeonWarfare.Scenes.World.Service.Chat.
    /// </summary>
    private static string ExpectedNamespace(string relativePath)
    {
        string directory = Path.GetDirectoryName(relativePath)!.Replace(Path.DirectorySeparatorChar, '/');
        return directory.Length == 0
            ? RootNamespace
            : $"{RootNamespace}.{directory.Replace('/', '.')}";
    }
}
