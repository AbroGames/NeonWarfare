using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.Scenes.Worlds;

// Separate from World only so that a test can build the container for every configuration;
// the game reaches the container through World alone
public class WorldServicesBuilder
{
    private const string SeveralLayersError = "{0} has more than one layer attribute: {1}";
    private const string NotConcreteError = "{0} has a layer attribute but is abstract or generic";
    private const string RootInterfaceError = "{0} implements {1}, which the composition root registers itself";
    private const string AmbiguousParameterError =
        "{0} takes a single {1}, which {2} implement: take IEnumerable<{1}> instead";

    private readonly IEnumerable<Type> _candidates;

    public WorldServicesBuilder() : this(typeof(WorldServicesBuilder).Assembly.GetTypes()) { }

    // A test passes its own fixture types: the game assembly cannot hold them
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

        // Eagerly: a service nobody takes in a constructor would silently never be created, and the constructors
        // that check their collections must fail here, not in the middle of a tick
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
        // By hand too: the one spawn besides EntitySpawner, shared by the client's state applier and the server's
        // load, which are of different layers
        services.AddSingleton<EntityRecordReader>();

        switch (setup)
        {
            case WorldSetup.RemoteClient remoteClient:
                services.AddSingleton(remoteClient.LocalPlayer);
                services.AddSingleton(remoteClient.LocalPlayerOwner);
                break;
            case WorldSetup.Host host:
                services.AddSingleton(host.SaveFiles);
                services.AddSingleton(host.LocalPlayer);
                services.AddSingleton(host.Admin);
                services.AddSingleton(host.ServerOwner);
                services.AddSingleton(host.LocalPlayerOwner);
                break;
            case WorldSetup.Dedicated dedicated:
                services.AddSingleton(dedicated.SaveFiles);
                services.AddSingleton(dedicated.Admin);
                services.AddSingleton(dedicated.ServerOwner);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(setup), setup, null);
        }

        HashSet<Type> rootTypes = services.Select(descriptor => descriptor.ServiceType).ToHashSet();
        foreach ((Type type, _) in selected)
        {
            services.AddSingleton(type);
            // So that a consumer takes every service of a kind as IEnumerable<I>. Only the game's interfaces and
            // those of a test's fixtures: the framework's and the engine's would be collections nobody means
            foreach (Type implemented in type.GetInterfaces().Where(IsOwnInterface(type)))
            {
                // MS.DI resolves a single I to the last registration: the service would silently replace the port
                if (rootTypes.Contains(implemented))
                {
                    throw new InvalidOperationException(
                        RootInterfaceError.FormatWith(type.FullName, implemented.FullName));
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

    // MS.DI resolves a single I to the last of its registrations: with several implementations it would pick one
    // silently, by the order of the scan
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
                    AmbiguousParameterError.FormatWith(type.FullName, parameter.ParameterType.Name, names));
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
