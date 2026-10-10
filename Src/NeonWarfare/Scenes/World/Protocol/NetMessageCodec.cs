using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using Humanizer;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using RepliCAT;
using NeonWarfare.Scripts.GlobalServices;

namespace NeonWarfare.Scenes.World.Protocol;

/// <summary>
/// The format of a network message: a little-endian <c>ushort</c> type id from <see cref="TypesMappingService"/>
/// followed by the MessagePack body.
/// </summary>
public sealed class NetMessageCodec
{
    private const int IdSize = sizeof(ushort);
    private const int CountSize = sizeof(int);
    // An id and at least one byte of MessagePack body
    private const int MinMessageSize = IdSize + 1;

    private const string IdOverflowError = "{0} has type id {1}, which does not fit the {2}-byte message id.";
    private const string TruncatedIdError = "The message is {0} bytes long, shorter than its {1}-byte type id.";
    private const string UnknownIdError = "Type id {0} is not in the mapping.";
    private const string NotAllowedError = "{0} is not allowed to come from the network.";
    private const string BrokenBodyError = "The body of {0} cannot be read.";
    private const string NullBodyError = "The body of {0} is nil.";
    private const string TruncatedCountError = "The section is {0} bytes long, shorter than its {1}-byte count.";
    private const string BadCountError = "The section claims {0} messages, but only {1} bytes follow the count.";

    private readonly TypesMappingService _mapping;
    private readonly MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            new IMessagePackFormatter[] { ColorFormatter.Instance },
            new IFormatterResolver[] { StandardResolver.Instance }))
        .WithSecurity(MessagePackSecurity.UntrustedData);

    public ulong ProtocolHash { get; }

    public NetMessageCodec(TypesMappingService mapping)
    {
        _mapping = mapping;
        ProtocolHash = new ProtocolHasher(new Replicator(mapping)).Compute(mapping.Types);
    }

    public void Write(IBufferWriter<byte> output, object message)
    {
        Type type = message.GetType();
        int id = _mapping.GetIdByType(type);
        if (id > ushort.MaxValue)
        {
            throw new InvalidOperationException(IdOverflowError.FormatWith(type.FullName, id, IdSize));
        }

        BinaryPrimitives.WriteUInt16LittleEndian(output.GetSpan(IdSize), (ushort) id);
        output.Advance(IdSize);
        MessagePackSerializer.Serialize(type, output, message, _options);
    }

    public byte[] Encode(object message)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Write(buffer, message);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// A section: a little-endian <c>int</c> count followed by that many messages, each already encoded by
    /// <see cref="Encode"/>.
    /// </summary>
    public void WriteSection(IBufferWriter<byte> output, IReadOnlyCollection<ReadOnlyMemory<byte>> encoded)
    {
        BinaryPrimitives.WriteInt32LittleEndian(output.GetSpan(CountSize), encoded.Count);
        output.Advance(CountSize);
        foreach (ReadOnlyMemory<byte> message in encoded)
        {
            output.Write(message.Span);
        }
    }

    /// <exception cref="NetMessageFormatException">
    /// A truncated or impossible count, or any message <see cref="Read"/> rejects.
    /// </exception>
    public IReadOnlyList<object> ReadSection(ReadOnlyMemory<byte> data, IReadOnlySet<Type> allowedTypes,
        out int bytesRead)
    {
        if (data.Length < CountSize)
        {
            throw new NetMessageFormatException(TruncatedCountError.FormatWith(data.Length, CountSize));
        }

        int count = BinaryPrimitives.ReadInt32LittleEndian(data.Span);
        int remaining = data.Length - CountSize;
        // Checked before the list is sized by the count: the count comes from an untrusted peer
        if (count < 0 || count > remaining / MinMessageSize)
        {
            throw new NetMessageFormatException(BadCountError.FormatWith(count, remaining));
        }

        var messages = new List<object>(count);
        int offset = CountSize;
        for (int i = 0; i < count; i++)
        {
            messages.Add(Read(data[offset..], allowedTypes, out int messageBytes));
            offset += messageBytes;
        }

        bytesRead = offset;
        return messages;
    }

    /// <summary>
    /// Both sides treat the peer as untrusted: a type outside <paramref name="allowedTypes"/> is rejected before
    /// its body is read.
    /// </summary>
    /// <exception cref="NetMessageFormatException">Unknown or not allowed type, or a broken body.</exception>
    public object Read(ReadOnlyMemory<byte> data, IReadOnlySet<Type> allowedTypes, out int bytesRead)
    {
        Type type = ReadType(data.Span);
        if (!allowedTypes.Contains(type))
        {
            throw new NetMessageFormatException(NotAllowedError.FormatWith(type.FullName));
        }

        return ReadBody(data, type, out bytesRead);
    }

    private Type ReadType(ReadOnlySpan<byte> data)
    {
        if (data.Length < IdSize)
        {
            throw new NetMessageFormatException(TruncatedIdError.FormatWith(data.Length, IdSize));
        }

        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(data);
        try
        {
            return _mapping.GetTypeById(id);
        }
        catch (KeyNotFoundException e)
        {
            throw new NetMessageFormatException(UnknownIdError.FormatWith(id), e);
        }
    }

    private object ReadBody(ReadOnlyMemory<byte> data, Type type, out int bytesRead)
    {
        var reader = new MessagePackReader(data[IdSize..]);
        object message;
        try
        {
            message = MessagePackSerializer.Deserialize(type, ref reader, _options);
        }
        catch (MessagePackSerializationException e)
        {
            throw new NetMessageFormatException(BrokenBodyError.FormatWith(type.FullName), e);
        }

        if (message == null)
        {
            throw new NetMessageFormatException(NullBodyError.FormatWith(type.FullName));
        }

        bytesRead = IdSize + (int) reader.Consumed;
        return message;
    }
}
