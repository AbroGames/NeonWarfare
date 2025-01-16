using System;
using System.Reflection;
using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerTypes;

public class StaticMethodPacketListener : IPacketListener
{
    public Type PacketType { get; }
    private readonly Delegate _delegate;
    private bool _isActive = true;
    public bool IsActive => _isActive;
    
    public StaticMethodPacketListener(MethodInfo method)
    {
        if (!ValidateMethod(method))
        {
            throw new ArgumentException($"Method signature of {method.Name} is not valid for {GetType().Name}");
        }
        
        PacketType = method.GetParameters()[0].ParameterType;
        
        var actionType = typeof(Action<>).MakeGenericType(PacketType);
        _delegate = Delegate.CreateDelegate(actionType, method);
    }

    public bool CanAccept(IPacket packet)
    {
        return packet is not null && PacketType.IsInstanceOfType(packet);
    }

    public void Deliver(IPacket packet)
    {
        if (!_isActive)
        {
            return;
        }
        
        if (!CanAccept(packet))
        {
            throw new ArgumentException($"Invalid message type: {packet.GetType().Name}. Expected: {PacketType.Name}");
        }

        // Вызов делегата с передачей сообщения
        _delegate.DynamicInvoke(packet);
    }

    public void Cancel()
    {
        _isActive = false;
    }

    private static bool ValidateMethod(MethodInfo method)
    {
        var isStatic = method.IsStatic;
        var onlyOneParameter = method.GetParameters().Length == 1;
        if (!onlyOneParameter)
        {
            return false;
        }
        
        var parameterIsMessage = method.GetParameters()[0].ParameterType.IsAssignableTo(typeof(IPacket));
        var isVoid = method.ReturnType == typeof(void);
        
        return isStatic && parameterIsMessage && isVoid;
    }
}
