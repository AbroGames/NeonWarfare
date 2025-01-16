using System;
using System.Collections;
using System.IO;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization.Binary.Serializers;

public class ArrayHandler : PayloadHandler
{
    public override Type PayloadType { get; }
    public Type ElementType => PayloadType.GetElementType();
    private PayloadHandler _arrayElementHandler;

    public ArrayHandler(Type arrayType)
    {
        if(!arrayType.IsArray)
            throw new ArgumentException($"Type {arrayType} is not an array");
        
        PayloadType = arrayType;

        if (!PayloadHandler.TryGetHandler(ElementType, out _arrayElementHandler))
        {
            throw new IOException($"No handler registered for type {ElementType}");
        }
    }


    public override object Default() => default;

    public override object Read(BinaryReader r)
    {
        var length = r.ReadInt32();
        var elements = Array.CreateInstance(ElementType, length);

        for (int i = 0; i < length; i++)
        {
            elements.SetValue(_arrayElementHandler.Read(r), i);
        }
        
        return elements;
    }

    public override void Write(BinaryWriter w, object v)
    {
        var valueType = v.GetType();
        if (!valueType.IsArray)
            throw new ArgumentException($"Type of {v} is not an array");
        
        if(valueType.GetElementType() != ElementType)
            throw new ArgumentException($"Element type of {v} is not serializable by {ElementType} serializer");
        
        var array = (Array)v;
        var length = array.Length;
        
        w.Write(length);

        foreach (var element in array)
        {
            _arrayElementHandler.Write(w, element);
        }
    }

    public override IList ReadList(BinaryReader r, int size)
    {
        throw new NotImplementedException();
    }

    public override Array ReadArray(BinaryReader r, int size)
    {
        throw new NotImplementedException();
    }

    public override void WriteList(BinaryWriter w, IList list)
    {
        throw new NotImplementedException();
    }

    public override void WriteArray(BinaryWriter w, Array array)
    {
        throw new NotImplementedException();
    }

    public override object Clone(object o)
    {
        throw new NotImplementedException();
    }

    public override IList CloneList(IList list)
    {
        throw new NotImplementedException();
    }
}
