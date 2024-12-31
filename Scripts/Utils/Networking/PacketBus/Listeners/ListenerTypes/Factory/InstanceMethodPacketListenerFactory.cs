using System;
using System.Reflection;
using NeonWarfare.Utils.InstanceRouting;
using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Utils.Networking;

public class InstanceMethodPacketListenerFactory : IPacketListenerFactory
{
    private readonly InstanceRouter _router;

    public InstanceMethodPacketListenerFactory(InstanceRouter router)
    {
        _router = router;
    }

    public bool IsSourceAcceptable(object source)
    {
        if(source is not MethodInfo method)
            return false;
        
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

    public IPacketListener CreateDestination(object source)
    {
        if(!IsSourceAcceptable(source))
            throw new ArgumentException();
    
        var listener = new InstanceMethodPacketListener((MethodInfo)source, _router);
        return listener;
    }
}