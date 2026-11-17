using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.Chat.ChatCommands;
using NeonWarfare.Scenes.Worlds.Infra.ClientNetwork;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;

namespace NeonWarfare.Scenes.Worlds;

// Separate from World only so that a test can build the container for every configuration;
// the game reaches the container through World alone
public class WorldServicesBuilder
{
    private const string SeveralLayersError = "{0} has more than one layer attribute: {1}";
    private const string NotConcreteError = "{0} has a layer attribute but is abstract or generic";
    private const string NoSaveFilesError = "A World with the ServerNetwork layer needs the save files.";
    private const string NoLocalPlayerError = "A World with the Presentation layer needs the local player.";
    private const string UnexpectedLocalPlayerError = "A World without the Presentation layer has no local player.";
    private const string NoLocalPlayerOwnerError = "A World with the local player needs its owner.";
    private const string UnexpectedLocalPlayerOwnerError = "A World without the local player has no owner of it.";
    private const string NoAdminError = "A World with the Simulation layer needs the admin, even one without a uid.";
    private const string UnexpectedAdminError = "A World without the Simulation layer has no admin.";
    private const string NoDedicatedServerOwnerError = "A dedicated server World needs the owner of its process.";
    private const string UnexpectedDedicatedServerOwnerError = "Only a dedicated server World has a process owner.";

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
        // Only a server World saves
        if (dependencies.SaveFiles != null)
        {
            services.AddSingleton(dependencies.SaveFiles);
        }
        else if (layers.HasFlag(WorldLayer.ServerNetwork))
        {
            throw new ArgumentException(NoSaveFilesError, nameof(dependencies));
        }
        if (layers.HasFlag(WorldLayer.Presentation) != (dependencies.LocalPlayer != null))
        {
            string error = dependencies.LocalPlayer == null ? NoLocalPlayerError : UnexpectedLocalPlayerError;
            throw new ArgumentException(error, nameof(dependencies));
        }
        if ((dependencies.LocalPlayer != null) != (dependencies.LocalPlayerOwner != null))
        {
            string error = dependencies.LocalPlayerOwner == null
                ? NoLocalPlayerOwnerError
                : UnexpectedLocalPlayerOwnerError;
            throw new ArgumentException(error, nameof(dependencies));
        }
        if (dependencies.LocalPlayer != null)
        {
            services.AddSingleton(dependencies.LocalPlayer);
            services.AddSingleton(dependencies.LocalPlayerOwner);
        }
        if (layers.HasFlag(WorldLayer.Simulation) != (dependencies.Admin != null))
        {
            string error = dependencies.Admin == null ? NoAdminError : UnexpectedAdminError;
            throw new ArgumentException(error, nameof(dependencies));
        }
        if (dependencies.Admin != null)
        {
            services.AddSingleton(dependencies.Admin);
        }
        bool isDedicated = layers.HasFlag(WorldLayer.Simulation) && !layers.HasFlag(WorldLayer.Presentation);
        if (isDedicated != (dependencies.DedicatedServerOwner != null))
        {
            string error = dependencies.DedicatedServerOwner == null
                ? NoDedicatedServerOwnerError
                : UnexpectedDedicatedServerOwnerError;
            throw new ArgumentException(error, nameof(dependencies));
        }
        if (dependencies.DedicatedServerOwner != null)
        {
            services.AddSingleton(dependencies.DedicatedServerOwner);
        }
        services.AddSingleton(root);
        // By hand, not by a layer attribute: the registry is the world's own state rather than a service of one
        // layer. Every layer reads it through IEntityFinder, only the spawning one registers
        services.AddSingleton<EntityRegistry>();
        services.AddSingleton<IEntityFinder>(provider => provider.GetRequiredService<EntityRegistry>());
        // By hand too: the one spawn besides EntitySpawner, shared by the client's state applier and the server's
        // load, which are of different layers
        services.AddSingleton<EntityRecordReader>();

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
