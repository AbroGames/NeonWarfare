using System;
using System.Reflection;
using NeonWarfare.Utils.InstanceRouting;
using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Utils.Networking;

public class InstanceMethodPacketListener : IPacketListener
{
    public Type PacketType { get; }
    public bool IsActive => _isActive;
    
    private readonly Delegate _delegate;
    private bool _isActive = true;
    private readonly InstanceRouter _router;
    private readonly Type _instanceType;
    
    public InstanceMethodPacketListener(MethodInfo method, InstanceRouter router)
    {
        if (!ValidateMethod(method))
        {
            throw new ArgumentException($"Method signature of {method.Name} is not valid for {GetType().Name}");
        }
        _instanceType = method.DeclaringType;
        
        _router = router;
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

        object key;
        key = packet;
        
        var instance = _router.Resolve(_instanceType, key);
        if (instance == null)
        {
            throw new InvalidOperationException("Instance resolver returned null.");
        }
        
        _delegate.DynamicInvoke(instance, packet);
    }

    public void Cancel()
    {
        _isActive = false;
    }

    private static bool ValidateMethod(MethodInfo method)
    {
        var isInstance = method.IsStatic;
        var onlyOneParameter = method.GetParameters().Length == 1;
        if (!onlyOneParameter)
        {
            return false;
        }
        
        var parameterIsMessage = method.GetParameters()[0].ParameterType.IsAssignableTo(typeof(IPacket));
        var isVoid = method.ReturnType == typeof(void);
        
        return isInstance && parameterIsMessage && isVoid;
    }
}