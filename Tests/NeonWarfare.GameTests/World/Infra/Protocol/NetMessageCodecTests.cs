using System.Buffers;
using System.Reflection;
using GdUnit4;
using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.Protocol;

[TestSuite]
public class NetMessageCodecTests
{
    // One value per primary-constructor parameter type of a command or an event. A new parameter type fails
    // the round-trip test until it is added here — together with its formatter, if MessagePack lacks one.
    private static readonly Dictionary<Type, object> SampleByType = new()
    {
        [typeof(string)] = "sample",
        [typeof(long)] = 1_700_000_000L,
        [typeof(ulong)] = 0xFEDC_BA98_7654_3210UL,
        [typeof(Color)] = new Color(0.1f, 0.2f, 0.3f, 0.4f),
    };

    [TestCase]
    [RequireGodotRuntime]
    public void CommandsAndEvents_RoundTrip()
    {
        TypesMappingService mapping = CreateMapping();
        var codec = new NetMessageCodec(mapping, []);
        HashSet<Type> messages = MessageTypes<Command>()
            .Concat(MessageTypes<Event>())
            .ToHashSet();
        var failures = new List<string>();

        foreach (Type type in messages)
        {
            object? message = CreateSample(type, failures);
            if (message == null)
            {
                continue;
            }

            var buffer = new ArrayBufferWriter<byte>();
            codec.Write(buffer, message);
            object read = codec.Read(buffer.WrittenMemory, messages, out int bytesRead);

            if (!read.Equals(message))
            {
                failures.Add($"{type.Name}: read back as {read}, written {message}");
            }

            if (bytesRead != buffer.WrittenCount)
            {
                failures.Add($"{type.Name}: {bytesRead} of {buffer.WrittenCount} bytes read");
            }
        }

        AssertThat(failures).IsEmpty();
    }

    // Guards the scan: an empty one would let the two tests above pass vacuously
    [TestCase]
    [RequireGodotRuntime]
    public void MessageTypes_IncludeTheClientCommands()
    {
        AssertThat(MessageTypes<Command>())
            .Contains(typeof(JoinRequestCommand), typeof(SendChatMessageCommand));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CommandsAndEvents_AreMappedToUshortIds()
    {
        TypesMappingService mapping = CreateMapping();
        var failures = new List<string>();

        foreach (Type type in MessageTypes<Command>()
                     .Concat(MessageTypes<Event>()))
        {
            if (!mapping.Types.Contains(type))
            {
                failures.Add($"{type.Name} is not in the mapping");
            }
            else if (mapping.GetIdByType(type) > ushort.MaxValue)
            {
                failures.Add($"{type.Name} has id {mapping.GetIdByType(type)}, which does not fit a ushort");
            }
        }

        AssertThat(failures).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_TypeOutsideAllowed_Throws()
    {
        var codec = new NetMessageCodec(CreateMapping(), []);
        var buffer = new ArrayBufferWriter<byte>();
        codec.Write(buffer, new PlayerJoinedEvent(1, "uid", "nick"));

        AssertRejected(() => codec.Read(buffer.WrittenMemory, AllowedCommands(), out _));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_UnknownId_Throws()
    {
        TypesMappingService mapping = CreateMapping();
        var codec = new NetMessageCodec(mapping, []);
        byte[] data = BitConverter.GetBytes((ushort) mapping.Types.Count);

        AssertRejected(() => codec.Read(data, AllowedCommands(), out _));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_TruncatedBody_Throws()
    {
        var codec = new NetMessageCodec(CreateMapping(), []);
        var buffer = new ArrayBufferWriter<byte>();
        codec.Write(buffer, new SendChatMessageCommand("hello"));
        ReadOnlyMemory<byte> truncated = buffer.WrittenMemory[..^1];

        AssertRejected(() => codec.Read(truncated, AllowedCommands(), out _));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Section_RoundTrips()
    {
        var codec = new NetMessageCodec(CreateMapping(), []);
        object[] messages = [new SendChatMessageCommand("hello"), new PlayerJoinedEvent(1, "uid", "nick")];
        HashSet<Type> allowed = [typeof(SendChatMessageCommand), typeof(PlayerJoinedEvent)];

        foreach (object[] section in new[] { [], messages })
        {
            var buffer = new ArrayBufferWriter<byte>();
            List<ReadOnlyMemory<byte>> encoded = section
                .Select(message => new ReadOnlyMemory<byte>(codec.Encode(message)))
                .ToList();
            codec.WriteSection(buffer, encoded);

            IReadOnlyList<object> read = codec.ReadSection(buffer.WrittenMemory, allowed, out int bytesRead);

            AssertThat(read).ContainsExactly(section);
            AssertThat(bytesRead).IsEqual(buffer.WrittenCount);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadSection_BrokenCount_Throws()
    {
        var codec = new NetMessageCodec(CreateMapping(), []);
        byte[] message = codec.Encode(new SendChatMessageCommand("hello"));

        // Too short for a count; a negative count; more messages than the bytes could ever hold; two messages
        // claimed, one written
        byte[][] broken =
        [
            [1, 0, 0],
            [..BitConverter.GetBytes(-1), ..message],
            [..BitConverter.GetBytes(int.MaxValue), ..message],
            [..BitConverter.GetBytes(2), ..message],
        ];
        foreach (byte[] data in broken)
        {
            AssertRejected(() => codec.ReadSection(data, AllowedCommands(), out _));
        }
    }

    // The game fills the mapping the same way, in BaseRootStarter
    internal static TypesMappingService CreateMapping()
    {
        var mapping = new TypesMappingService();
        mapping.SetTypes(typeof(JoinRequestCommand).Assembly.GetTypes().ToList());
        return mapping;
    }

    // Not AssertThrown: it reports the inner exception, and the codec wraps the one it caught
    internal static void AssertRejected(Action read)
    {
        Exception? thrown = null;
        try
        {
            read();
        }
        catch (Exception e)
        {
            thrown = e;
        }

        AssertThat(thrown).IsInstanceOf<NetMessageFormatException>();
    }

    private static HashSet<Type> AllowedCommands() => [typeof(JoinRequestCommand), typeof(SendChatMessageCommand)];

    private static IEnumerable<Type> MessageTypes<TBase>() =>
        typeof(TBase).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(TBase)) && type is { IsAbstract: false, IsNested: false });

    private static object? CreateSample(Type type, List<string> failures)
    {
        ConstructorInfo[] constructors = type.GetConstructors();
        if (constructors.Length != 1)
        {
            failures.Add($"{type.Name}: expected one public constructor, found {constructors.Length}");
            return null;
        }

        var arguments = new List<object>();
        foreach (ParameterInfo parameter in constructors[0].GetParameters())
        {
            if (!SampleByType.TryGetValue(parameter.ParameterType, out object? sample))
            {
                failures.Add($"{type.Name}.{parameter.Name}: add a sample {parameter.ParameterType} to "
                             + nameof(SampleByType));
                return null;
            }

            arguments.Add(sample);
        }

        return constructors[0].Invoke(arguments.ToArray());
    }
}
