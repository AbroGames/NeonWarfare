using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Conventions;

/// <summary>
/// The rules from Docs/Code-style.md that fit in a few lines each and that the compiler has no opinion
/// about.
/// </summary>
public class CodeStyleTests
{
    private const string EventSuffix = "Event";

    private const string ResourcePathPrefix = "res://";

    /// <summary>The file that owns every global import, see Docs/Repository-structure.md.</summary>
    private const string GlobalUsingsFile = "Src/NeonWarfare/Scripts/GlobalUsings.cs";

    /// <summary>The two ways to reach a resource by path instead of through a scene storage.</summary>
    private const string GodotClass = "GD";

    private const string ResourceLoaderClass = "ResourceLoader";

    private static readonly string[] LoadMethods = ["Load", "LoadThreadedRequest", "LoadThreadedGet"];

    /// <summary>
    /// "Events are named <c>&lt;What&gt;Event</c> with the side spelled out" from Docs/Code-style.md.
    /// The suffix is what tells a subscription apart from a method call at the use site, where the two
    /// look the same.
    /// </summary>
    [Fact]
    public void Events_AreNamedWithTheEventSuffix()
    {
        FailureReport report = new("Events without the Event suffix");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            IEnumerable<MemberDeclarationSyntax> events = file.Nodes<MemberDeclarationSyntax>()
                .Where(member => member is EventFieldDeclarationSyntax or EventDeclarationSyntax);

            foreach (MemberDeclarationSyntax declaration in events)
            {
                foreach (string name in CSharpFile.DeclaredNames(declaration))
                {
                    if (!name.EndsWith(EventSuffix, StringComparison.Ordinal))
                    {
                        report.Add($"{file.Describe(declaration)}: '{name}' must end with {EventSuffix}");
                    }
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// "New global imports are added only there" from Docs/Services.md. A global using is invisible at
    /// the use site — the code simply says <c>Di.Process(this)</c> — so scattering the declarations makes
    /// it impossible to tell where a name comes from.
    /// </summary>
    [Fact]
    public void GlobalUsings_LiveInASingleFile()
    {
        FailureReport report = new($"global using outside {GlobalUsingsFile}");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            if (string.Equals(file.RelativePath, GlobalUsingsFile, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (UsingDirectiveSyntax directive in file.Nodes<UsingDirectiveSyntax>())
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
                {
                    report.Add($"{file.Describe(directive)}: {directive.ToString().Trim()}");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// "Obtaining any scene for instantiation starts here, not with <c>GD.Load</c>" from
    /// Docs/Dependency-injection.md: the prototypes live in the CheckedAbstractStorage storages, which are
    /// wired in the editor and validated by <c>[NotNull]</c>. A path written in code is checked by
    /// nothing — it fails at runtime, on the one screen that loads it.
    /// </summary>
    [Fact]
    public void Resources_AreNotLoadedByPath()
    {
        FailureReport report = new("Resources reached by path instead of through a scene storage");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            foreach (InvocationExpressionSyntax invocation in file.Nodes<InvocationExpressionSyntax>())
            {
                string receiver = CSharpFile.ReceiverOf(invocation);
                string receiverClass = invocation.Expression is MemberAccessExpressionSyntax access
                    ? RightmostName(access.Expression)
                    : string.Empty;
                bool loadsByPath =
                    (receiverClass == GodotClass && LoadMethods.Contains(CSharpFile.CalledName(invocation)))
                    || receiverClass == ResourceLoaderClass;

                if (loadsByPath)
                {
                    report.Add($"{file.Describe(invocation)}: {receiver}." +
                               $"{CSharpFile.CalledName(invocation)}(...) — take the scene from a " +
                               $"CheckedAbstractStorage instead");
                }
            }

            // Without a semantic model a bare Load(...) cannot be told from a method of the class itself,
            // so the import that would make it reach Godot is what gets reported.
            foreach (UsingDirectiveSyntax directive in file.Nodes<UsingDirectiveSyntax>())
            {
                bool importsLoader = directive.Name is not null
                                     && (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                                         || directive.Alias is not null)
                                     && RightmostName(directive.Name) is GodotClass or ResourceLoaderClass;

                if (importsLoader)
                {
                    report.Add($"{file.Describe(directive)}: {directive.ToString().Trim()} — hides the " +
                               $"loader from this check; take the scene from a CheckedAbstractStorage instead");
                }
            }

            foreach (LiteralExpressionSyntax literal in file.Nodes<LiteralExpressionSyntax>())
            {
                if (literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && literal.Token.ValueText.StartsWith(ResourcePathPrefix, StringComparison.Ordinal))
                {
                    report.Add($"{file.Describe(literal)}: '{literal.Token.ValueText}' — a res:// path in " +
                               $"code is checked by nothing and survives every rename");
                }
            }

            foreach (InterpolatedStringExpressionSyntax interpolated in
                     file.Nodes<InterpolatedStringExpressionSyntax>())
            {
                if (interpolated.Contents.FirstOrDefault() is InterpolatedStringTextSyntax text
                    && text.TextToken.ValueText.StartsWith(ResourcePathPrefix, StringComparison.Ordinal))
                {
                    report.Add($"{file.Describe(interpolated)}: {interpolated} — a res:// path in code is " +
                               $"checked by nothing and survives every rename");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// <c>GD</c> for <c>GD</c>, <c>Godot.GD</c> and <c>global::Godot.GD</c>: without a semantic model the
    /// qualification is only spelling, and comparing it whole let a qualified call slip through.
    /// </summary>
    private static string RightmostName(ExpressionSyntax expression) =>
        expression switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => expression.ToString(),
        };
}
