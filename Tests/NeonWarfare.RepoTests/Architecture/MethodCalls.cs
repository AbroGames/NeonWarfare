using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The game methods a method body calls or takes as a delegate. Methods of other assemblies are left out: they are
/// never resolved, so a framework type needs no assembly resolver.
/// </summary>
public static class MethodCalls
{
    /// <param name="AsDelegate">
    /// The target is taken by <c>ldftn</c> / <c>ldvirtftn</c>: it runs whenever the delegate is invoked, not here.
    /// </param>
    public sealed record Call(MethodDefinition Target, bool AsDelegate);

    public static IEnumerable<Call> Of(MethodDefinition method)
    {
        if (!method.HasBody)
        {
            yield break;
        }

        foreach (Instruction instruction in method.Body.Instructions)
        {
            if (instruction.Operand is not MethodReference called
                || instruction.OpCode.Code is not (Code.Call or Code.Callvirt or Code.Newobj
                    or Code.Ldftn or Code.Ldvirtftn)
                || GameAssembly.Instance.Find(called.DeclaringType) == null
                || called.Resolve() is not { } target)
            {
                continue;
            }

            yield return new Call(target, instruction.OpCode.Code is Code.Ldftn or Code.Ldvirtftn);
        }
    }

    /// <summary>
    /// Every method that reaches one of <paramref name="targets"/>: calls it, takes it as a delegate, or does so with
    /// a method that does, at any depth. The targets themselves are not included unless they reach each other.
    /// </summary>
    public static HashSet<MethodDefinition> Reaching(IReadOnlySet<MethodDefinition> targets)
    {
        List<MethodDefinition> all = GameAssembly.Instance.Types
            .SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .ToList();
        Dictionary<MethodDefinition, List<MethodDefinition>> callees = all.ToDictionary(
            method => method,
            method => Of(method).Select(call => call.Target).ToList());

        HashSet<MethodDefinition> reaching = [];
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (MethodDefinition method in all.Where(method => !reaching.Contains(method)))
            {
                if (callees[method].Any(callee => targets.Contains(callee) || reaching.Contains(callee)))
                {
                    reaching.Add(method);
                    grew = true;
                }
            }
        }

        return reaching;
    }
}
