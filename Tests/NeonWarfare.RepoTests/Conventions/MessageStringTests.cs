using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Conventions;

/// <summary>
/// "Strings" from Docs/Code-style.md: a log template and an exception message are written at the call, a constant is
/// only for what the player sees, TrKeys and configuration. A wall of message constants at the top of a class pushes
/// the logic down and sends the reader of a <c>throw</c> to another screen.
/// <br/>
/// Syntax only: a constant declared in another file, or a logger reached through a property, is not seen.
/// </summary>
public class MessageStringTests
{
    private const string LoggerType = "ILogger";

    private const string ExceptionSuffix = "Exception";

    private const string FormatWithMethod = "FormatWith";

    private static readonly string[] LogMethods = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    /// <summary>
    /// <c>ProcessDeadChecker</c> logs the message its caller builds: the dedicated server names the dead process as its
    /// parent and says it shuts down.
    /// </summary>
    private static readonly string[] BuiltTemplateFiles = ["Src/GodotBox/Godot/Nodes/Process/ProcessDeadChecker.cs"];

    /// <summary>
    /// A constant template hides its parameters from the reader of the call, and <c>$"…"</c> turns Serilog's
    /// structured properties into plain text.
    /// </summary>
    [Fact]
    public void LogTemplates_AreLiteralsAtTheCall()
    {
        FailureReport report = new("Log templates that are not a string literal in the call");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            if (BuiltTemplateFiles.Contains(file.RelativePath))
            {
                continue;
            }

            foreach (ExpressionSyntax template in NonLiteralLogTemplates(file))
            {
                report.Add($"{file.Describe(template)}: {template} — write the template as a string literal in the " +
                           $"call");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void BuiltTemplateFiles_StillBuildTheirTemplate()
    {
        CrossCheck.AssertExemptionsExist(
            nameof(BuiltTemplateFiles),
            BuiltTemplateFiles,
            path => CSharpFile.LoadAll().Any(file => file.RelativePath == path && NonLiteralLogTemplates(file).Any()),
            "the file is gone or logs only literal templates");
    }

    [Fact]
    public void ExceptionMessages_AreWrittenInTheThrow()
    {
        FailureReport report = new("Exception messages built from a constant or FormatWith");

        foreach (CSharpFile file in CSharpFile.LoadAll())
        {
            HashSet<string> constants = StringConstants(file);

            foreach (ArgumentListSyntax arguments in ExceptionArguments(file))
            {
                foreach (ArgumentSyntax argument in arguments.Arguments)
                {
                    // A constant interpolated into $"…" is a value (a flag, a path), not the message itself
                    bool usesConstant = argument.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Any(name => constants.Contains(name.Identifier.ValueText)
                                     && !name.Ancestors().TakeWhile(node => node != argument)
                                         .OfType<InterpolationSyntax>().Any());
                    bool usesFormatWith = argument.DescendantNodesAndSelf()
                        .OfType<InvocationExpressionSyntax>()
                        .Any(call => CSharpFile.CalledName(call) == FormatWithMethod);

                    if (usesConstant || usesFormatWith)
                    {
                        report.Add($"{file.Describe(argument)}: {argument} — write the message as $\"…\" in the " +
                                   $"throw");
                    }
                }
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The arguments an exception is built from: <c>new …Exception(…)</c>, a target-typed <c>new(…)</c> that is thrown,
    /// and the base constructor call of an exception class.
    /// </summary>
    private static IEnumerable<ArgumentListSyntax> ExceptionArguments(CSharpFile file)
    {
        IEnumerable<ArgumentListSyntax?> created = file.Nodes<ObjectCreationExpressionSyntax>()
            .Where(creation => IsExceptionName(TypeName(creation.Type)))
            .Select(creation => creation.ArgumentList);
        IEnumerable<ArgumentListSyntax?> thrown = file.Nodes<ImplicitObjectCreationExpressionSyntax>()
            .Where(creation => creation.Parent is ThrowStatementSyntax or ThrowExpressionSyntax)
            .Select(creation => creation.ArgumentList);
        IEnumerable<ArgumentListSyntax?> primaryBase = file.Nodes<PrimaryConstructorBaseTypeSyntax>()
            .Where(baseType => IsExceptionName(TypeName(baseType.Type)))
            .Select(baseType => baseType.ArgumentList);
        IEnumerable<ArgumentListSyntax?> constructorBase = file.Nodes<ConstructorInitializerSyntax>()
            .Where(initializer => CSharpFile.DeclaringType(initializer) is { } type
                                  && IsExceptionName(type.Identifier.ValueText))
            .Select(initializer => initializer.ArgumentList);

        return created.Concat(thrown).Concat(primaryBase).Concat(constructorBase).OfType<ArgumentListSyntax>();
    }

    private static bool IsExceptionName(string name) => name.EndsWith(ExceptionSuffix, StringComparison.Ordinal);

    private static IEnumerable<ExpressionSyntax> NonLiteralLogTemplates(CSharpFile file)
    {
        HashSet<string> loggers = LoggerNames(file);
        HashSet<string> constants = StringConstants(file);

        foreach (InvocationExpressionSyntax invocation in file.Nodes<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver }
                || !loggers.Contains(receiver.Identifier.ValueText)
                || !LogMethods.Contains(CSharpFile.CalledName(invocation)))
            {
                continue;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count == 0)
            {
                continue;
            }

            ExpressionSyntax template = IsTemplateLike(arguments[0].Expression, constants) || arguments.Count == 1
                ? arguments[0].Expression
                : arguments[1].Expression;

            if (!IsLiteral(template))
            {
                yield return template;
            }
        }
    }

    /// <summary>Fields, locals and parameters declared as <c>ILogger</c>.</summary>
    private static HashSet<string> LoggerNames(CSharpFile file)
    {
        IEnumerable<string> variables = file.Nodes<VariableDeclarationSyntax>()
            .Where(declaration => TypeName(declaration.Type) == LoggerType)
            .SelectMany(declaration => declaration.Variables)
            .Select(variable => variable.Identifier.ValueText);
        IEnumerable<string> parameters = file.Nodes<ParameterSyntax>()
            .Where(parameter => parameter.Type is not null && TypeName(parameter.Type) == LoggerType)
            .Select(parameter => parameter.Identifier.ValueText);

        return variables.Concat(parameters).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Every <c>const string</c> of the file, fields and locals alike.</summary>
    private static HashSet<string> StringConstants(CSharpFile file)
    {
        IEnumerable<VariableDeclarationSyntax> fields = file.Nodes<FieldDeclarationSyntax>()
            .Where(field => field.Modifiers.Any(SyntaxKind.ConstKeyword))
            .Select(field => field.Declaration);
        IEnumerable<VariableDeclarationSyntax> locals = file.Nodes<LocalDeclarationStatementSyntax>()
            .Where(local => local.IsConst)
            .Select(local => local.Declaration);

        return fields.Concat(locals)
            .Where(declaration => declaration.Type is PredefinedTypeSyntax predefined
                                  && predefined.Keyword.IsKind(SyntaxKind.StringKeyword))
            .SelectMany(declaration => declaration.Variables)
            .Select(variable => variable.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Serilog's exception overloads take the exception first; anything that looks like a string is the template
    /// itself.
    /// </summary>
    private static bool IsTemplateLike(ExpressionSyntax expression, HashSet<string> constants) =>
        expression switch
        {
            LiteralExpressionSyntax or InterpolatedStringExpressionSyntax => true,
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) => true,
            IdentifierNameSyntax name => constants.Contains(name.Identifier.ValueText),
            InvocationExpressionSyntax call => CSharpFile.CalledName(call) == FormatWithMethod,
            _ => false,
        };

    /// <summary>A string literal, or literals joined by <c>+</c> to wrap a long template.</summary>
    private static bool IsLiteral(ExpressionSyntax expression) =>
        expression switch
        {
            LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) =>
                IsLiteral(binary.Left) && IsLiteral(binary.Right),
            ParenthesizedExpressionSyntax parenthesized => IsLiteral(parenthesized.Expression),
            _ => false,
        };

    private static string TypeName(TypeSyntax type) =>
        type switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            NullableTypeSyntax nullable => TypeName(nullable.ElementType),
            _ => type.ToString(),
        };
}
