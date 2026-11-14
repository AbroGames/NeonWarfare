using Mono.Cecil;
using NeonWarfare.RepoTests.Infrastructure;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The call sites of <c>PlayerCommandSender.Send&lt;TCommand&gt;</c>, the only sender of commands in the World.
/// </summary>
public static class CommandSends
{
    private const string Sender = WorldLayers.WorldNamespace + ".Infra.ClientNetwork.PlayerCommandSender";
    private const string SendMethod = "Send";

    public sealed record Site(TypeDefinition Caller, IMemberDefinition From, TypeReference Command);

    /// <summary>
    /// Throws when the sender or its method is gone: a rename must not leave the rules nothing to check. The set
    /// itself may be empty.
    /// </summary>
    public static IReadOnlyList<Site> All()
    {
        GameAssembly game = GameAssembly.Instance;
        TypeDefinition sender = game.FindByName(Sender) ?? throw new InvalidOperationException($"{Sender} is gone");
        if (!sender.Methods.Any(method => method is { Name: SendMethod, HasGenericParameters: true }))
        {
            throw new InvalidOperationException($"{Sender}.{SendMethod}<TCommand> is gone");
        }

        List<Site> sites = [];
        foreach (TypeDefinition type in game.Types)
        {
            foreach (TypeReferenceSite site in TypeReferences.Of(type))
            {
                if (site.Type.FullName != Sender || site.Via is not GenericInstanceMethod { Name: SendMethod } call)
                {
                    continue;
                }

                TypeReference command = call.GenericArguments[0] is GenericParameter parameter
                    ? throw new NotSupportedException(
                        $"{GameAssembly.Describe(site.From)} sends the type parameter {parameter.Name}: a generic " +
                        "sender is not supported by the command rules")
                    : call.GenericArguments[0];
                sites.Add(new Site(type, site.From, command));
            }
        }
        return sites;
    }
}
