using System.Reflection;
using System.Reflection.Emit;
using GdUnit4;
using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Protocol;

[TestSuite]
public class ProtocolHasherTests
{
    // Every run is a new process, so an input that differs between processes changes the recorded hash.
    // A deliberate change of the hash input changes it too, and then the constant is recorded anew.
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_FixedInput_GivesTheRecordedHash()
    {
        var mapping = new TypesMappingService();
        mapping.SetTypes([typeof(FixedMessage), typeof(FixedModel), typeof(FixedKind)]);
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, ["scene res://Fixed.tscn", "type Fixed.Node"]))
            .IsEqual(new ProtocolHash(10660722168259770158UL));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreType_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types.Append(typeof(ExtraMessage)).ToList(), Kinds))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreKind_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Append("scene res://Extra.tscn").ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    // The kind id is its index in the catalog
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SwappedKinds_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Reverse().ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    // A node type that stops being a kind (it became abstract, say) is still in the type list: only the kinds see it
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_TypeKindRemovedWithTheSameTypes_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Where(kind => !kind.StartsWith("type ")).ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_EnumMemberRenamed_ChangesHash()
    {
        AssertThat(HashOf(BuildEnum(typeof(int), ("Low", 1), ("Middle", 2), ("High", 3))))
            .IsNotEqual(HashOf(BuildEnum(typeof(int), BaseMembers)));
    }

    // Below the maximum value, so the width of the enum on the wire stays the same
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_EnumMemberAdded_ChangesHash()
    {
        AssertThat(HashOf(BuildEnum(typeof(int), [("None", 0), ..BaseMembers])))
            .IsNotEqual(HashOf(BuildEnum(typeof(int), BaseMembers)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_EnumMemberRemoved_ChangesHash()
    {
        AssertThat(HashOf(BuildEnum(typeof(int), ("Low", 1), ("High", 3))))
            .IsNotEqual(HashOf(BuildEnum(typeof(int), BaseMembers)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_EnumMemberRenumbered_ChangesHash()
    {
        AssertThat(HashOf(BuildEnum(typeof(int), ("Low", 0), ("Mid", 2), ("High", 3))))
            .IsNotEqual(HashOf(BuildEnum(typeof(int), BaseMembers)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_EnumUnderlyingTypeChanged_ChangesHash()
    {
        AssertThat(HashOf(BuildEnum(typeof(byte), BaseMembers)))
            .IsNotEqual(HashOf(BuildEnum(typeof(int), BaseMembers)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_EnumMembersDeclaredInAnotherOrder_KeepsHash()
    {
        AssertThat(HashOf(BuildEnum(typeof(int), ("High", 3), ("Low", 1), ("Mid", 2))))
            .IsEqual(HashOf(BuildEnum(typeof(int), BaseMembers)));
    }

    private static readonly (string Name, int Value)[] BaseMembers = [("Low", 1), ("Mid", 2), ("High", 3)];

    private static ProtocolHash HashOf(Type enumType)
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        return new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping)).Compute([enumType], Kinds);
    }

    // The same enum of another build has the same full name, so each version lives in a dynamic assembly of its own
    private static Type BuildEnum(Type underlying, params (string Name, int Value)[] members)
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"Build{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        EnumBuilder builder = assembly.DefineDynamicModule("Protocol")
            .DefineEnum("Fixed.Reason", TypeAttributes.Public, underlying);
        foreach ((string name, int value) in members)
        {
            builder.DefineLiteral(name, Convert.ChangeType(value, underlying));
        }

        return builder.CreateType();
    }

    private static readonly string[] Kinds =
    [
        "scene res://Safe.tscn", "scene res://Battle.tscn",
        "type NeonWarfare.Scenes.Worlds.Features.Players.OnlinePlayersStorage",
    ];

    // Stands for a message type added in a newer build
    private class ExtraMessage;

    [MessagePackObject]
    public record FixedMessage(
        [property: Key(0)] string Text,
        [property: Key(1)] int Count,
        [property: Key(2)] FixedKind Kind);

    public enum FixedKind : byte
    {
        First = 1,
        Second = 2,
    }

    public class FixedModel
    {
        [Replicated] public string Name = "";
        [Replicated] public int Score;
    }
}
