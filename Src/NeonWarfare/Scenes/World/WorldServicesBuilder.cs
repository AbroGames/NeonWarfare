using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Chat.ChatCommands;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;

namespace NeonWarfare.Scenes.World;

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

    public ServiceProvider Build(WorldLayer layers, WorldDependencies dependencies, WorldRoot root)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dependencies.Time);
        services.AddSingleton(dependencies.Codec);
        services.AddSingleton(dependencies.Replicator);
        services.AddSingleton(dependencies.Frames);
        services.AddSingleton(dependencies.Scenes);
        services.AddSingleton(dependencies.Entities);
        services.AddSingleton(dependencies.ClientsConnection);
        services.AddSingleton(dependencies.ServerConnection);
        services.AddSingleton(root);
        // By hand, not by a layer attribute: the registry is the world's own state rather than a service of one
        // layer. Every layer reads it through IEntityFinder, only the spawning one registers
        services.AddSingleton<EntityRegistry>();
        services.AddSingleton<IEntityFinder>(provider => provider.GetRequiredService<EntityRegistry>());

        var selected = ScanWorldServices()
            .Where(service => layers.HasFlag(service.Attribute.Layer))
            .ToList();
        foreach ((Type type, _) in selected)
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
        foreach ((Type type, _) in selected)
        {
            provider.GetRequiredService(type);
        }

        if (layers.HasFlag(WorldLayer.ClientNetwork))
        {
            IEnumerable<object> presentations = selected
                .Where(service => service.Attribute is PresentationAttribute)
                .Select(service => provider.GetRequiredService(service.Type));
            HashSet<Type> eventTypes = _candidates
                .Where(type => type.IsSubclassOf(typeof(Event)) && type is { IsNested: false, IsAbstract: false })
                .ToHashSet();
            provider.GetRequiredService<EventDispatcher>().Register(presentations, eventTypes);
        }

        if (layers.HasFlag(WorldLayer.SimulationFacade))
        {
            IEnumerable<IChatCommand> commands = selected
                .Select(service => provider.GetRequiredService(service.Type))
                .OfType<IChatCommand>();
            provider.GetRequiredService<ChatSimulationFacade>().Register(commands);
        }

        if (layers.HasFlag(WorldLayer.ServerNetwork))
        {
            IEnumerable<object> handlers = selected
                .Where(service => service.Attribute is CommandHandlerAttribute)
                .Select(service => provider.GetRequiredService(service.Type));
            provider.GetRequiredService<CommandHandlerRegistry>().Register(handlers);
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
