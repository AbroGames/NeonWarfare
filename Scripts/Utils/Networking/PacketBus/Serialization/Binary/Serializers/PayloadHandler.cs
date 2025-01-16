using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization.Binary.Serializers;

public abstract class PayloadHandler
{
    private static readonly IDictionary<Type, PayloadHandler> Handlers = new Dictionary<Type, PayloadHandler>();
    public abstract Type PayloadType { get; }
    
    internal static void Reload()
    {
        Handlers.Clear();

        var scannedSerializers = ReflectionExtensions.FindAllTypesThatDeriveFrom<PayloadHandler>();

        foreach (var type in scannedSerializers)
        {
            try
            {
                if (!type.IsAbstract)
                {
                    try
                    {
                        AddHandler((PayloadHandler)Activator.CreateInstance(type)!);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                    
            }
            catch
            {
                Log.Debug($"Failed to instantiate TagSerializer '{type.FullName}'.");
            }
        }
    }
    
    public static bool TryGetHandler(Type type, [NotNullWhen(true)] out PayloadHandler serializer)
    {
        if (Handlers.TryGetValue(type, out serializer))
            return true;

        if (type.IsArray)
        {
            try
            {
                var handler = new ArrayHandler(type);
                AddHandler(handler);
                serializer = handler;
                return true;
            }
            catch(Exception e)
            {
                throw new IOException($"Failed to get handler for type '{type.FullName}'.", e);
            }
        }

        return false;
    }
    
    internal static void AddHandler(PayloadHandler handler)
    {
        Handlers.Add(handler.PayloadType, handler);
    }
    public abstract object Default();
    public abstract object Read(BinaryReader r);
    public abstract void Write(BinaryWriter w, object v);
    public abstract IList ReadList(BinaryReader r, int size);
    public abstract Array ReadArray(BinaryReader r, int size);
    public abstract void WriteList(BinaryWriter w, IList list);
    public abstract void WriteArray(BinaryWriter w, Array array);
    public abstract object Clone(object o);
    public abstract IList CloneList(IList list);
}

public abstract class PayloadHandler<TValue> : PayloadHandler
{
    public sealed override Type PayloadType => typeof(TValue);
    public sealed override object Read(BinaryReader reader) => ReadGeneric(reader);
    public sealed override void Write(BinaryWriter writer, object value) => WriteGeneric(writer, (TValue)value);
    
    protected abstract TValue ReadGeneric(BinaryReader reader);
    protected abstract void WriteGeneric(BinaryWriter writer, TValue value);
    
    public override IList ReadList(BinaryReader r, int size)
    {
        var list = new List<TValue>(size);
        for (int i = 0; i < size; i++)
            list.Add(ReadGeneric(r));

        return list;
    }

    public override Array ReadArray(BinaryReader r, int size)
    {
        var array = new TValue[size];
        for (int i = 0; i < size; i++)
            array[i] = ReadGeneric(r);
        
        return array;
    }

    public override void WriteList(BinaryWriter w, IList list)
    {
        foreach (TValue t in list)
            WriteGeneric(w, t);
    }
    
    public override void WriteArray(BinaryWriter w, Array array)
    {
        foreach (TValue t in array)
            WriteGeneric(w, t);
    }

    public override object Clone(object o) => o;
    public override IList CloneList(IList list) => CloneList((IList<TValue>)list);
    public virtual IList CloneList(IList<TValue> list) => new List<TValue>(list);

    public override object Default() => default(TValue);
}

public class DelegatePayloadHandler<TValue> : PayloadHandler<TValue>
    where TValue : notnull
{
    protected Func<BinaryReader, TValue> _readDelegate;
    protected Action<BinaryWriter, TValue> _writeDelegate;

    public DelegatePayloadHandler(Func<BinaryReader, TValue> reader, Action<BinaryWriter, TValue> writer)
    {
        _readDelegate = reader;
        _writeDelegate = writer;
    }

    protected override TValue ReadGeneric(BinaryReader reader)
    {
        return _readDelegate(reader);
    }

    protected override void WriteGeneric(BinaryWriter writer, TValue value)
    {
        _writeDelegate(writer, value);
    }
}

public class ClassPayloadHandler<T> : DelegatePayloadHandler<T> where T : class
{
    protected Func<T, T> _clone;
    protected Func<T> _makeDefault;

    public ClassPayloadHandler(Func<BinaryReader, T> reader, Action<BinaryWriter, T> writer,
        Func<T, T> clone, Func<T> makeDefault = null) :
        base(reader, writer)
    {
        _clone = clone;
        _makeDefault = makeDefault;
    }

    public override object Clone(object o) => _clone((T)o);
    public override IList CloneList(IList<T> list) => list.Select(_clone).ToList();
    public override object Default() => _makeDefault!(); // If makeDefault is null, it's our job to handle default values to ensure this is never called
}
