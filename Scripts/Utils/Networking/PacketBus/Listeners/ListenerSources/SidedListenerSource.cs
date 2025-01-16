using System;
using System.Linq;
using System.Reflection;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerSources;

public class SidedListenerSource : IListenerSource
{
    private readonly ListenerSide _side;

    public SidedListenerSource(ListenerSide side)
    {
        _side = side;
    }
    
    public object[] GetDestinations()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(asm => asm.GetTypes());
        var methods = types.SelectMany(type => type.GetMethods());
        var markedMethods = methods.Where(method => method.GetCustomAttribute<PacketListenerAttribute>() is not null);
        var sidedMethods = markedMethods.Where(method => _side.HasFlag(method.GetCustomAttribute<PacketListenerAttribute>()!.Side));
        
        return sidedMethods.ToArray<object>();
    }
}
