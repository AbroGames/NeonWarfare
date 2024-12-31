using NeonWarfare.Utils.Networking.DestinationTypes;

namespace NeonWarfare.Utils.Networking;

public class CombinedPacketListenerFactory : IPacketListenerFactory
{
    private readonly IPacketListenerFactory[] _factories;

    public CombinedPacketListenerFactory(params IPacketListenerFactory[] factories)
    {
        _factories = factories;
    }
    
    public bool IsSourceAcceptable(object source)
    {
        return GetAcceptableFactoryFor(source) is not null;
    }

    public IPacketListener CreateDestination(object source)
    {
        return GetAcceptableFactoryFor(source).CreateDestination(source);
    }

    private IPacketListenerFactory GetAcceptableFactoryFor(object source)
    {
        foreach (var factory in _factories)
        {
            if (factory.IsSourceAcceptable(source))
                return factory;
        }
        
        return null;
    }
}