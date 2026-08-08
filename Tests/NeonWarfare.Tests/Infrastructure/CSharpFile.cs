using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A game source file parsed into a syntax tree. The convention tests need to tell a declaration from
/// a call, an attribute from a comment and a string literal from an identifier — matching regexes
/// against lines gets all three wrong, and Docs/Code-style.md is written in exactly those terms.
/// <br/>
/// Only the parser is used, never a compilation: there is no reference to the game project and no
/// GodotSharp, so nothing here can drag the engine into the test process.
/// </summary>
public sealed class CSharpFile
{
    private const string ParseErrorMessage =
        "{Path}: the file did not parse as C#. Either the file is broken, or the language moved ahead " +
        "of the Microsoft.CodeAnalysis.CSharp version in NeonWarfare.Tests.csproj — bump the package. " +
        "Errors:\n{Errors}";

    /// <summary>
    /// Latest, not a pinned version: the game project follows the SDK, so pinning here would start
    /// reporting new syntax as a parse error.
    /// </summary>
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    /// <summary>Several tests read the same files, and parsing all 147 of them repeatedly is wasteful.</summary>
    private static readonly ConcurrentDictionary<string, CSharpFile> Parsed = new(StringComparer.Ordinal);

    private CSharpFile(string path, SyntaxNode root)
    {
        Path = path;
        Root = root;
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    public SyntaxNode Root { get; }

    public static CSharpFile Load(string path) =>
        Parsed.GetOrAdd(System.IO.Path.GetFullPath(path), Parse);

    /// <summary>Every game source file, parsed.</summary>
    public static IReadOnlyList<CSharpFile> LoadAll() =>
        RepositoryPaths.SourceFiles().Select(Load).ToList();

    /// <summary>One-based line number of a node, matching what an editor shows.</summary>
    public int LineOf(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    /// <summary><c>Docs/Foo.cs:42</c> — clickable in the failure report.</summary>
    public string Describe(SyntaxNode node) => $"{RelativePath}:{LineOf(node)}";

    /// <summary>All nodes of a kind anywhere in the file.</summary>
    public IEnumerable<T> Nodes<T>() where T : SyntaxNode => Root.DescendantNodes().OfType<T>();

    /// <summary>
    /// The method a node sits in, or <c>null</c> at file or type level. Used to say which wrapper an
    /// RPC is called from and which method reaches for the command line.
    /// </summary>
    public static MethodDeclarationSyntax? EnclosingMethod(SyntaxNode node) =>
        node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

    /// <summary>
    /// The name a call is made through: <c>Net</c> for <c>Net.IsServer()</c>, <c>Services.Net</c> for
    /// <c>Services.Net.IsServer()</c>, empty for an unqualified <c>IsServer()</c>.
    /// </summary>
    public static string ReceiverOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax access
            ? access.Expression.ToString()
            : string.Empty;

    /// <summary>
    /// The type a node is declared in, or <c>null</c> at file level. A DI attribute is a statement about
    /// its class, not about the file it happens to share with others.
    /// </summary>
    public static TypeDeclarationSyntax? DeclaringType(SyntaxNode node) =>
        node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

    /// <summary>
    /// An attribute's name as it is written, without the optional <c>Attribute</c> suffix and without
    /// any qualification: both <c>[Child]</c> and <c>[KludgeBox.ChildAttribute]</c> come back as
    /// <c>Child</c>.
    /// </summary>
    public static string AttributeName(AttributeSyntax attribute)
    {
        string name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => attribute.Name.ToString(),
        };

        const string suffix = "Attribute";
        return name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length
            ? name[..^suffix.Length]
            : name;
    }

    /// <summary>
    /// Fields and properties carrying one of the given attributes, one entry per declared name — a
    /// single field declaration can introduce several.
    /// </summary>
    public IEnumerable<AttributedMember> MembersWith(params string[] attributeNames)
    {
        foreach (MemberDeclarationSyntax member in Nodes<MemberDeclarationSyntax>())
        {
            if (member is not (FieldDeclarationSyntax or PropertyDeclarationSyntax))
            {
                continue;
            }

            foreach (AttributeSyntax attribute in member.AttributeLists.SelectMany(list => list.Attributes))
            {
                if (!attributeNames.Contains(AttributeName(attribute)))
                {
                    continue;
                }

                foreach (string name in DeclaredNames(member))
                {
                    yield return new AttributedMember(member, name, attribute, DeclaringType(member));
                }
            }
        }
    }

    /// <summary>The names a field or property declaration introduces.</summary>
    public static IEnumerable<string> DeclaredNames(MemberDeclarationSyntax member) =>
        member switch
        {
            FieldDeclarationSyntax field =>
                field.Declaration.Variables.Select(variable => variable.Identifier.ValueText),
            PropertyDeclarationSyntax property => [property.Identifier.ValueText],
            EventFieldDeclarationSyntax @event =>
                @event.Declaration.Variables.Select(variable => variable.Identifier.ValueText),
            EventDeclarationSyntax @event => [@event.Identifier.ValueText],
            _ => [],
        };

    /// <summary>The called method's name, whatever the call is qualified with.</summary>
    public static string CalledName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => string.Empty,
        };

    private static CSharpFile Parse(string path)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), ParseOptions, path);
        List<Diagnostic> errors = tree.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();

        // A tree with parse errors still yields nodes, just not the ones that were meant to be
        // there — a test running on it would pass while checking nothing.
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(ParseErrorMessage
                .Replace("{Path}", RepositoryPaths.Relative(path))
                .Replace("{Errors}", string.Join("\n", errors.Select(error => error.ToString()))));
        }

        return new CSharpFile(path, tree.GetRoot());
    }
}

/// <summary>
/// A field or property found by its attribute: the declaration, the one name this entry is about, the
/// attribute that matched and the type the member belongs to.
/// </summary>
public sealed record AttributedMember(
    MemberDeclarationSyntax Declaration,
    string Name,
    AttributeSyntax Attribute,
    TypeDeclarationSyntax? DeclaringType);
