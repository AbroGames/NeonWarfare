using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// Builds the container of the world services, see Docs/World.md#the-container.
/// Separate from <see cref="World"/> only so a test can build the container for every configuration.
/// The game reaches the container through <see cref="World"/> alone.
/// </summary>
public class WorldServicesBuilder
{
    private readonly IEnumerable<Type> _candidates;

    public WorldServicesBuilder() : this(typeof(WorldServicesBuilder).Assembly.GetTypes()) { }

    /// <summary>A test passes its own fixture types, because the game assembly cannot hold them.</summary>
    public WorldServicesBuilder(IEnumerable<Type> candidates)
    {
        _candidates = candidates;
    }

    public ServiceProvider Build(WorldSetup setup, WorldDependencies dependencies, WorldRoot root)
    {
        WorldLayer layers = setup.Layers;
        List<(Type Type, WorldServiceAttribute Attribute)> selected = SelectWorldServices(layers);
        CheckSingleInterfaceParameters(selected);
        ServiceCollection services = GetServices(setup, dependencies, root, selected);
        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Every service is created eagerly, so a service nobody takes still exists.
        // A constructor that rejects its collection then fails here, not in the middle of a tick.
        foreach ((Type type, _) in selected)
        {
            provider.GetRequiredService(type);
        }

        return provider;
    }

    private ServiceCollection GetServices(
        WorldSetup setup, WorldDependencies dependencies, WorldRoot root,
        IEnumerable<(Type Type, WorldServiceAttribute Attribute)> selected)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dependencies.Time);
        services.AddSingleton(dependencies.Codec);
        services.AddSingleton(dependencies.ProtocolHash);
        services.AddSingleton(dependencies.Replicator);
        services.AddSingleton(dependencies.Frames);
        services.AddSingleton(dependencies.Scenes);
        services.AddSingleton(dependencies.Entities);
        services.AddSingleton(dependencies.ClientsConnection);
        services.AddSingleton(dependencies.ServerConnection);
        services.AddSingleton(root);
        // The registry is registered by hand, because it is the world's own state and not a service of one layer.
        services.AddSingleton<EntityRegistry>();
        services.AddSingleton<IEntityFinder>(provider => provider.GetRequiredService<EntityRegistry>());
        // The reader is registered by hand too, because the client's state applier and the server's load share it, and
        // they are of different layers.
        services.AddSingleton<EntityRecordReader>();

        switch (setup)
        {
            case WorldSetup.RemoteClient remoteClient:
                services.AddSingleton(remoteClient.LocalPlayer);
                services.AddSingleton(remoteClient.LocalPlayerOwner);
                break;
            case WorldSetup.Host host:
                services.AddSingleton(host.SaveFiles);
                services.AddSingleton(host.SaveFile);
                services.AddSingleton(host.LocalPlayer);
                services.AddSingleton(host.Admin);
                services.AddSingleton(host.ProtectedPlayer);
                services.AddSingleton(host.ServerOwner);
                services.AddSingleton(host.LocalPlayerOwner);
                break;
            case WorldSetup.Dedicated dedicated:
                services.AddSingleton(dedicated.SaveFiles);
                services.AddSingleton(dedicated.SaveFile);
                services.AddSingleton(dedicated.Admin);
                services.AddSingleton(dedicated.ProtectedPlayer);
                services.AddSingleton(dedicated.ServerOwner);
                break;
            default:
                throw new UnreachableException();
        }

        HashSet<Type> rootTypes = services.Select(descriptor => descriptor.ServiceType).ToHashSet();
        foreach ((Type type, _) in selected)
        {
            services.AddSingleton(type);
            // A consumer takes every service of a kind as IEnumerable<I>.
            // Only the interfaces of the game and of a test's fixtures count. Those of the framework and the engine
            // would make collections nobody means.
            foreach (Type implemented in type.GetInterfaces().Where(IsOwnInterface(type)))
            {
                // MS.DI resolves a single I to the last registration, so the service would silently replace the port.
                if (rootTypes.Contains(implemented))
                {
                    throw new InvalidOperationException(
                        $"{type.FullName} implements {implemented.FullName}, which the composition root registers "
                        + "itself");
                }
                services.AddSingleton(implemented, provider => provider.GetRequiredService(type));
            }
        }
        return services;
    }

    private static Func<Type, bool> IsOwnInterface(Type service) =>
        implemented => implemented.Assembly == service.Assembly
                       || implemented.Assembly == typeof(WorldServicesBuilder).Assembly;

    private List<(Type Type, WorldServiceAttribute Attribute)> SelectWorldServices(WorldLayer layers)
    {
        return ScanWorldServices()
            .Where(service => layers.HasFlag(service.Attribute.Layer))
            .ToList();
    }

    // Rejects a single I parameter when several services implement I, because MS.DI would silently hand over the last
    // registration, which depends on the order of the scan.
    private static void CheckSingleInterfaceParameters(
        IEnumerable<(Type Type, WorldServiceAttribute Attribute)> selected)
    {
        List<Type> types = selected.Select(service => service.Type).ToList();
        Dictionary<Type, List<Type>> implementationsByInterface = types
            .SelectMany(type => type.GetInterfaces()
                .Where(IsOwnInterface(type))
                .Select(implemented => (Interface: implemented, Implementation: type)))
            .GroupBy(pair => pair.Interface, pair => pair.Implementation)
            .Where(group => group.Count() > 1)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (Type type in types)
        {
            foreach (ParameterInfo parameter in type.GetConstructors().SelectMany(ctor => ctor.GetParameters()))
            {
                if (!implementationsByInterface.TryGetValue(parameter.ParameterType, out List<Type> implementations))
                {
                    continue;
                }

                string names = string.Join(", ", implementations.Select(implementation => implementation.Name));
                throw new InvalidOperationException(
                    $"{type.FullName} takes a single {parameter.ParameterType.Name}, which {names} implement: take "
                    + $"IEnumerable<{parameter.ParameterType.Name}> instead");
            }
        }
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
                throw new InvalidOperationException($"{type.FullName} has more than one layer attribute: {names}");
            }
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                throw new InvalidOperationException(
                    $"{type.FullName} has a layer attribute but is abstract or generic");
            }
            yield return (type, attributes[0]);
        }
    }
}
