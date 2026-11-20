using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Conventions;

/// <summary>
/// "Methods" from Docs/Code-style.md: <c>Event</c> is a protocol record, <c>On…</c> is a handler of a C# event,
/// a C# event has no suffix. Next to <c>PlayerJoinedEvent</c> a C# event named
/// <c>LocalPlayerJoinedEvent</c> reads as one more network message, and a method named <c>PeerConnectedEvent</c> reads
/// as an event field.
/// </summary>
public class EventNamingTests
{
    private const string EventSuffix = "Event";

    private const string HandlerPrefix = "On";

    /// <summary>The session code; the menus are left to the conventions of their own.</summary>
    private static readonly string[] Folders =
    [
        "Src/NeonWarfare/Scenes/Root/",
        "Src/NeonWarfare/Scenes/Game/",
        "Src/NeonWarfare/Scenes/Worlds/",
    ];

    [Fact]
    public void Events_HaveNoEventSuffix()
    {
        FailureReport report = new("C# events with the Event suffix");

        foreach (CSharpFile file in ScopedFiles())
        {
            IEnumerable<MemberDeclarationSyntax> events = file.Nodes<MemberDeclarationSyntax>()
                .Where(member => member is EventFieldDeclarationSyntax or EventDeclarationSyntax);

            foreach (MemberDeclarationSyntax declaration in events)
            {
                foreach (string name in CSharpFile.DeclaredNames(declaration))
                {
                    if (name.EndsWith(EventSuffix, StringComparison.Ordinal))
                    {
                        report.Add($"{file.Describe(declaration)}: '{name}' — the Event suffix is for protocol " +
                                   $"records, name the C# event without it");
                    }
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// A protected <c>On…</c> is a hook the base class calls once something has happened
    /// (<c>BaseHostGameStarter.OnLoadFailed</c>): a reaction too, only overridden instead of subscribed.
    /// </summary>
    [Fact]
    public void OnMethods_ArePrivateOrProtected()
    {
        FailureReport report = new($"Public or internal {HandlerPrefix}… methods");

        foreach (CSharpFile file in ScopedFiles())
        {
            foreach (MethodDeclarationSyntax method in file.Nodes<MethodDeclarationSyntax>())
            {
                string name = method.Identifier.ValueText;
                if (IsHandlerName(name) && IsVisibleOutside(method))
                {
                    report.Add($"{file.Describe(method)}: '{name}' — {HandlerPrefix}… is a handler or a hook, a " +
                               $"method called from outside is named by what it does");
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// This is what keeps out a handler named <c>PeerConnectedEvent</c>; any other method ending with <c>Event</c>
    /// (<c>PublishEvent</c>) is about a protocol record. Only a method group of this file is checked: a lambda has no
    /// name, and without a semantic model a name from elsewhere cannot be told from a field or a number.
    /// </summary>
    [Fact]
    public void SubscribedMethods_StartWithOn()
    {
        FailureReport report = new($"Subscribed methods not named {HandlerPrefix}…");

        foreach (CSharpFile file in ScopedFiles())
        {
            HashSet<string> methods = Methods(file).Select(method => method.Name).ToHashSet(StringComparer.Ordinal);

            foreach (AssignmentExpressionSyntax assignment in file.Nodes<AssignmentExpressionSyntax>())
            {
                if (!assignment.IsKind(SyntaxKind.AddAssignmentExpression)
                    && !assignment.IsKind(SyntaxKind.SubtractAssignmentExpression))
                {
                    continue;
                }

                string? name = assignment.Right switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access =>
                        access.Name.Identifier.ValueText,
                    _ => null,
                };
                if (name != null && methods.Contains(name) && !IsHandlerName(name))
                {
                    report.Add($"{file.Describe(assignment)}: '{name}' handles {assignment.Left} — name it " +
                               $"{HandlerPrefix}<Event>");
                }
            }
        }

        report.AssertEmpty();
    }

    private static List<CSharpFile> ScopedFiles()
    {
        List<CSharpFile> files = CSharpFile.LoadAll()
            .Where(file => Folders.Any(folder => file.RelativePath.StartsWith(folder, StringComparison.Ordinal)))
            .ToList();
        foreach (string folder in Folders)
        {
            // Without it a moved folder would leave the rules nothing to check
            Assert.Contains(files, file => file.RelativePath.StartsWith(folder, StringComparison.Ordinal));
        }
        return files;
    }

    private static IEnumerable<(SyntaxNode Declaration, string Name)> Methods(CSharpFile file) =>
        file.Nodes<MethodDeclarationSyntax>()
            .Select(method => ((SyntaxNode) method, method.Identifier.ValueText))
            .Concat(file.Nodes<LocalFunctionStatementSyntax>()
                .Select(function => ((SyntaxNode) function, function.Identifier.ValueText)));

    /// <summary><c>OnPeerConnected</c>, not <c>Online</c>.</summary>
    private static bool IsHandlerName(string name) =>
        name.Length > HandlerPrefix.Length
        && name.StartsWith(HandlerPrefix, StringComparison.Ordinal)
        && char.IsUpper(name[HandlerPrefix.Length]);

    /// <summary>
    /// An interface member without an access modifier is public, a class member without one is private.
    /// </summary>
    private static bool IsVisibleOutside(MethodDeclarationSyntax method) =>
        method.Parent is InterfaceDeclarationSyntax
        || method.Modifiers.Any(modifier =>
            modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword));
}
