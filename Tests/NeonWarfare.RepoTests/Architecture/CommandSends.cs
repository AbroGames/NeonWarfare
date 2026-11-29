using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The call sites of <c>Send&lt;TCommand&gt;</c> of a command sender: <c>PlayerCommandSender</c>, the only sender of
/// commands in the World, and the World's <c>ICommandSender</c> entry point, which <c>World</c> implements explicitly
/// by forwarding to it.
/// </summary>
public static class CommandSends
{
    private const string PlayerSender = WorldLayers.WorldNamespace + ".Infra.Client.PlayerCommandSender";
    private const string WorldSender = WorldLayers.WorldNamespace + ".World/ICommandSender";
    private const string SendMethod = "Send";

    private static readonly string[] Senders = [PlayerSender, WorldSender];

    public sealed record Site(TypeDefinition Caller, IMemberDefinition From, TypeReference Command);

    /// <summary>
    /// Throws when a sender or its method is gone or nothing calls them: the rules must never have nothing to check.
    /// </summary>
    public static IReadOnlyList<Site> All()
    {
        GameAssembly game = GameAssembly.Instance;
        foreach (string name in Senders)
        {
            TypeDefinition sender = game.FindByName(name) ?? throw new InvalidOperationException($"{name} is gone");
            if (!sender.Methods.Any(method => method is { Name: SendMethod, HasGenericParameters: true }))
            {
                throw new InvalidOperationException($"{name}.{SendMethod}<TCommand> is gone");
            }
        }

        List<Site> sites = [];
        foreach (TypeDefinition type in game.Types)
        {
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (!Senders.Contains(site.Type.FullName)
                    || site.Via is not GenericInstanceMethod { Name: SendMethod } call)
                {
                    continue;
                }

                if (call.GenericArguments[0] is GenericParameter parameter)
                {
                    // One sender forwarding to another: its own callers name the command
                    if (IsSender(site.From)) continue;

                    throw new NotSupportedException(
                        $"{GameAssembly.Describe(site.From)} sends the type parameter {parameter.Name}: a generic " +
                        "sender is not supported by the command rules");
                }
                sites.Add(new Site(type, site.From, call.GenericArguments[0]));
            }
        }
        if (sites.Count == 0)
        {
            throw new InvalidOperationException(
                $"Nothing calls {SendMethod}<TCommand> of {string.Join(", ", Senders)}");
        }
        return sites;
    }

    private static bool IsSender(IMemberDefinition member) =>
        member is MethodDefinition method
        && (method.Name == SendMethod && Senders.Contains(method.DeclaringType.FullName)
            || method.Overrides.Any(sender => sender.Name == SendMethod
                && Senders.Contains(sender.DeclaringType.FullName)));
}
