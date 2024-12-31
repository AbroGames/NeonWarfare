using System;
using System.Reflection;
using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Utils.Networking;

public class StaticMethodPacketListenerFactory : IPacketListenerFactory
{
    public bool IsSourceAcceptable(object source)
    {
        if(source is not MethodInfo methodInfo)
            return false;
        
        if(methodInfo.GetParameters().Length != 1)
            return false;
        
        var isStatic = methodInfo.IsStatic;
        var isVoid = methodInfo.ReturnType == typeof(void);
        var paramIsMessage = methodInfo.GetParameters()[0].ParameterType.IsAssignableTo(typeof(IPacket));
        
        return isStatic && isVoid && paramIsMessage;
    }

    public IPacketListener CreateDestination(object source)
    {
        if(!IsSourceAcceptable(source))
            throw new ArgumentException();
        
        var destination = new StaticMethodPacketListener((MethodInfo)source);
        return destination;
    }
}