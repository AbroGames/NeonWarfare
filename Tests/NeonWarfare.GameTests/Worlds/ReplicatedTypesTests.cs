using System.Reflection;
using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Features.Players;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds;

// RepliCAT's own tests run without the engine, so a node as the replicated root and the game's type mapping are
// tried only here. A model error (an unsupported member type, a readonly member the client would have to replace,
// no parameterless constructor) surfaces at the first delta or apply, which a type with default values reaches too
[TestSuite]
public class ReplicatedTypesTests
{
    private const BindingFlags DeclaredInstanceMembers =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    [TestCase]
    [RequireGodotRuntime]
    public void ReplicatedTypes_RoundTripWithDefaults()
    {
        var replicator = new Replicator(NetMessageCodecTests.CreateMapping());
        List<Type> types = typeof(PlayersStorage).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, ContainsGenericParameters: false } && DeclaresReplicated(type))
            .ToList();
        // Otherwise a rename of the attribute would leave this test nothing to check
        AssertThat(types).Contains(typeof(PlayersStorage), typeof(PlayersSessionStorage), typeof(PlayerModel));

        var failures = new List<string>();
        foreach (Type type in types)
        {
            try
            {
                RoundTrip(replicator, type);
            }
            catch (Exception e)
            {
                failures.Add($"{type.FullName}: {e.GetType().Name}: {e.Message}");
            }
        }

        AssertThat(string.Join('\n', failures)).IsEmpty();
    }

    private static void RoundTrip(Replicator replicator, Type type)
    {
        object server = Create(type);
        ReplicationBaseline baseline = replicator.CreateBaseline(server);
        if (replicator.TryWriteDelta(baseline, out byte[] delta))
        {
            replicator.Apply(Create(type), delta);
        }

        if (!replicator.TryWriteSnapshot(baseline, out byte[] snapshot))
        {
            throw new InvalidOperationException("No snapshot after the first delta.");
        }

        replicator.Apply(Create(type), snapshot);
    }

    private static object Create(Type type)
    {
        object instance = Activator.CreateInstance(type, nonPublic: true)!;
        return instance is GodotObject godotObject ? AutoFree(godotObject)! : instance;
    }

    private static bool DeclaresReplicated(Type type)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            if (current.GetMembers(DeclaredInstanceMembers).Any(member =>
                    member is FieldInfo or PropertyInfo && member.IsDefined(typeof(ReplicatedAttribute), false)))
            {
                return true;
            }
        }

        return false;
    }
}
