using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.ServerNetwork;

namespace NeonWarfare.Scenes.World.Composition;

// Separate from World only so that a test can build the container for every configuration;
// the game reaches the container through World alone
public class WorldServicesBuilder
{
    private const string SeveralLayersError = "{0} has more than one layer attribute: {1}";
    private const string NotConcreteError = "{0} has a layer attribute but is abstract or generic";

    private readonly IEnumerable<Type> _candidates;

    public WorldServicesBuilder() : this(typeof(WorldServicesBuilder).Assembly.GetTypes()) { }

    // A test passes its own fixture types: the game assembly cannot hold them
    public WorldServicesBuilder(IEnumerable<Type> candidates)
    {
        _candidates = candidates;
    }

    public ServiceProvider Build(WorldServiceGroups groups, WorldDependencies dependencies)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dependencies.Time);
        services.AddSingleton(dependencies.Persistence);
        services.AddSingleton(dependencies.Session);
        services.AddSingleton(dependencies.Codec);

        List<Type> selected = ScanWorldServices()
            .Where(service => service.Attribute.AnnotatedClassBelongsTo(groups))
            .Select(service => service.Type)
            .ToList();
        foreach (Type type in selected)
        {
            services.AddSingleton(type);
        }

        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Eagerly, or a service nobody takes in a constructor (a Presentation with only event handlers)
        // would silently never be created
        foreach (Type type in selected)
        {
            provider.GetRequiredService(type);
        }

        // The console input exists only with ServerHud, so only then is there a console to send events to
        if (groups.HasServerHud)
        {
            provider.GetRequiredService<EventOutbox>().AddConsole();
        }
        return provider;
    }

    private IEnumerable<(Type Type, WorldServiceAttribute Attribute)> ScanWorldServices()
    {
        foreach (Type type in _candidates)
        {
            var attributes = type.GetCustomAttributes<WorldServiceAttribute>(inherit: false).ToList();
            if (attributes.Count == 0) continue;

            if (attributes.Count > 1)
            {
                string names = string.Join(", ", attributes.Select(attribute => attribute.GetType().Name));
                throw new InvalidOperationException(SeveralLayersError.FormatWith(type.FullName, names));
            }
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                throw new InvalidOperationException(NotConcreteError.FormatWith(type.FullName));
            }
            yield return (type, attributes[0]);
        }
    }
}
