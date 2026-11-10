using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NeonWarfare.RepoTests.Infrastructure;

/// <summary>
/// The compiled game assembly, read once by Mono.Cecil as metadata and IL. It is never loaded into the
/// test process, so GodotSharp is not either — the same principle as the compilation in
/// <c>GodotBoxIndependenceTests</c>.
/// <br/>
/// Cecil is not thread-safe, so every test class using it joins the <see cref="Collection"/> collection
/// and xUnit runs them one at a time.
/// </summary>
public sealed class GameAssembly
{
    public const string Collection = nameof(GameAssembly);

    private static readonly Lazy<GameAssembly> Loaded = new(Load);

    private readonly Dictionary<string, TypeDefinition> _typesByFullName;

    private GameAssembly(ModuleDefinition module)
    {
        Module = module;
        Types = module.Types.SelectMany(WithNested).Where(type => type.Name != "<Module>").ToList();
        _typesByFullName = Types.ToDictionary(type => type.FullName, StringComparer.Ordinal);
    }

    public static GameAssembly Instance => Loaded.Value;

    public ModuleDefinition Module { get; }

    /// <summary>Every type of the game, nested and compiler-generated ones included.</summary>
    public IReadOnlyList<TypeDefinition> Types { get; }

    /// <summary>
    /// The game's own definition of a referenced type, or null for a type of another assembly. A lookup
    /// by name, not <c>Resolve()</c>: it needs no assembly resolver and cannot fail on a framework type.
    /// </summary>
    public TypeDefinition? Find(TypeReference reference)
    {
        TypeReference element = reference.GetElementType();
        return element.Scope == Module ? FindByName(element.FullName) : null;
    }

    public TypeDefinition? FindByName(string cecilFullName) =>
        _typesByFullName.GetValueOrDefault(cecilFullName);

    /// <summary>The outermost type a nested type lives in — the one carrying the namespace in Cecil.</summary>
    public static TypeReference Outermost(TypeReference type)
    {
        while (type.DeclaringType != null)
        {
            type = type.DeclaringType;
        }

        return type;
    }

    /// <summary>The type and every type enclosing it, innermost first.</summary>
    public static IEnumerable<TypeDefinition> SelfAndEnclosing(TypeDefinition type)
    {
        for (TypeDefinition? current = type; current != null; current = current.DeclaringType)
        {
            yield return current;
        }
    }

    /// <summary>A lambda closure, a state machine or another type the compiler made, nested in it or not.</summary>
    public static bool IsCompilerGenerated(TypeDefinition type) =>
        SelfAndEnclosing(type).Any(owner => owner.Name.StartsWith('<') || HasCompilerGeneratedAttribute(owner));

    /// <summary>A lambda body, a local function or a member the compiler wrote, or any method of such a type.</summary>
    public static bool IsCompilerGenerated(MethodDefinition method) =>
        method.Name.StartsWith('<')
        || HasCompilerGeneratedAttribute(method)
        || IsCompilerGenerated(method.DeclaringType);

    /// <summary><c>Outer.Inner</c> without the namespace, for failure messages.</summary>
    public static string ShortName(TypeReference type) =>
        type.DeclaringType == null ? type.Name : $"{ShortName(type.DeclaringType)}.{type.Name}";

    /// <summary>
    /// <c>Src/…/File.cs:42 (Type::Member)</c> — where a failure message sends the reader. The line is the
    /// first sequence point of the method, or of any method of the type for a field or a type; without
    /// symbols the location is left out.
    /// </summary>
    public static string Describe(IMemberDefinition member)
    {
        TypeDefinition type = member as TypeDefinition ?? member.DeclaringType;
        string name = member is TypeDefinition ? ShortName(type) : $"{ShortName(type)}::{member.Name}";

        // An async or iterator method keeps its body, and its lines, in the state machine's MoveNext.
        SequencePoint? point = (member as MethodDefinition is { } method ? FirstSequencePoint(method) : null)
                               ?? SelfAndEnclosing(type).SelectMany(owner => owner.Methods)
                                   .Select(FirstSequencePoint)
                                   .FirstOrDefault(found => found != null);

        return point == null
            ? name
            : $"{RepositoryPaths.Relative(point.Document.Url)}:{point.StartLine} ({name})";
    }

    private static bool HasCompilerGeneratedAttribute(ICustomAttributeProvider provider) =>
        provider.CustomAttributes.Any(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    private static SequencePoint? FirstSequencePoint(MethodDefinition method)
    {
        if (method.DebugInformation.HasSequencePoints)
        {
            return method.DebugInformation.SequencePoints.FirstOrDefault(point => !point.IsHidden);
        }

        MethodDefinition? moveNext = method.CustomAttributes
            .Where(a => a.AttributeType.Name.EndsWith("StateMachineAttribute", StringComparison.Ordinal))
            .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value)
            .OfType<TypeReference>()
            .Select(stateMachine => stateMachine.Resolve()?.Methods.FirstOrDefault(m => m.Name == "MoveNext"))
            .FirstOrDefault(found => found != null);
        return moveNext?.DebugInformation.SequencePoints.FirstOrDefault(point => !point.IsHidden);
    }

    private static IEnumerable<TypeDefinition> WithNested(TypeDefinition type) =>
        type.NestedTypes.SelectMany(WithNested).Prepend(type);

    private static GameAssembly Load()
    {
        string path = RepositoryPaths.GameAssemblyPath;
        string pdb = Path.ChangeExtension(path, ".pdb");

        // The game output folder holds every package assembly, GodotSharp and RepliCAT included, so the
        // few references a test resolves (base types of nodes, RepliCAT members) are all found there.
        DefaultAssemblyResolver resolver = new();
        resolver.AddSearchDirectory(Path.GetDirectoryName(path)!);

        // In memory and immediate: the file is not kept open against the next game build, and nothing is
        // read lazily later from several threads.
        ModuleDefinition module = ModuleDefinition.ReadModule(path, new ReaderParameters
        {
            AssemblyResolver = resolver,
            InMemory = true,
            ReadingMode = ReadingMode.Immediate,
            ReadSymbols = File.Exists(pdb),
        });
        return new GameAssembly(module);
    }
}
