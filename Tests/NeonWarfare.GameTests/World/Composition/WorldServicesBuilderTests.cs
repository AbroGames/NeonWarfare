using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.CommandHandlers;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;
using NeonWarfare.Scenes.World.Presentations;
using NeonWarfare.Scenes.World.Simulations;
using static GdUnit4.Assertions;
using GameWorld = NeonWarfare.Scenes.World.World;

namespace NeonWarfare.GameTests.World.Composition;

// In the engine on purpose: the point is that MS.DI works inside a Godot process
[TestSuite]
public class WorldServicesBuilderTests
{
    private static readonly WorldServiceGroups Client = new(false, PresentationScope.Full);
    private static readonly WorldServiceGroups Host = new(true, PresentationScope.Full);
    private static readonly WorldServiceGroups DedicatedWithServerHud =
        new(true, PresentationScope.RequiredByServerHud);
    private static readonly WorldServiceGroups HeadlessDedicated = new(true, PresentationScope.None);

    // Handler → facade → simulation are constructor-injected and ValidateOnBuild rejects a broken chain:
    // a present handler means all three are there, an absent ChatSimulation means none is

    [TestCase]
    [RequireGodotRuntime]
    public void Build_Client_HasOnlyPresentation()
    {
        using ServiceProvider provider = Build(Client);

        AssertThat(provider.GetService<ChatSimulation>()).IsNull();
        AssertThat(provider.GetService<ChatPresentation>()).IsNotNull();
    }

    // The two give the same chat services: ChatPresentation is RequiredByServerHud
    [TestCase]
    [RequireGodotRuntime]
    public void Build_HostAndDedicatedWithServerHud_HaveSimulationAndPresentation()
    {
        foreach (WorldServiceGroups groups in new[] { Host, DedicatedWithServerHud })
        {
            using ServiceProvider provider = Build(groups);

            AssertThat(provider.GetService<SendChatMessageHandler>()).IsNotNull();
            AssertThat(provider.GetService<ChatPresentation>()).IsNotNull();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_HeadlessDedicated_HasOnlySimulation()
    {
        using ServiceProvider provider = Build(HeadlessDedicated);

        AssertThat(provider.GetService<SendChatMessageHandler>()).IsNotNull();
        AssertThat(provider.GetService<ChatPresentation>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_CreatesServicesNobodyRequests()
    {
        UnrequestedPresentation.Created = 0;

        using ServiceProvider provider = new WorldServicesBuilder([typeof(UnrequestedPresentation)])
            .Build(Host, Dependencies());

        AssertThat(UnrequestedPresentation.Created).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Build_AnyConfiguration_HasQueries()
    {
        foreach (WorldServiceGroups groups in new[] { Client, Host, DedicatedWithServerHud, HeadlessDedicated })
        {
            using ServiceProvider provider = new WorldServicesBuilder([typeof(FixtureQuery)])
                .Build(groups, Dependencies());

            // A bool: AssertThat dispatches dynamically and cannot bind a private fixture type
            AssertThat(provider.GetService<FixtureQuery>() != null).IsTrue();
        }
    }

    // The layer table lets facades take facades; no test of ours looks for cycles, the container does
    [TestCase]
    [RequireGodotRuntime]
    public void Build_FacadeCycle_Throws()
    {
        string message = "";
        try
        {
            new WorldServicesBuilder([typeof(CyclicFacadeA), typeof(CyclicFacadeB)])
                .Build(Host, Dependencies())
                .Dispose();
        }
        catch (AggregateException exception)
        {
            message = exception.Message;
        }

        AssertThat(message).Contains("circular dependency");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_OutsideTree_Succeeds()
    {
        GameWorld world = AutoFree(new GameWorld())!;

        AssertThat(world.InitPreReady(Host, new PersistenceModel(), new SessionModel())).IsSame(world);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void InitPreReady_InsideTree_Throws()
    {
        GameWorld world = AutoFree(new GameWorld())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(world);

        AssertThrown(() => world.InitPreReady(Host, new PersistenceModel(), new SessionModel()))
            .IsInstanceOf<InvalidOperationException>();
    }

    private static ServiceProvider Build(WorldServiceGroups groups) =>
        new WorldServicesBuilder().Build(groups, Dependencies());

    private static WorldDependencies Dependencies() =>
        new(TimeProvider.System, new PersistenceModel(), new SessionModel());

    [Query]
    private class FixtureQuery;

    [Simulation(Facade = true)]
    private class CyclicFacadeA(CyclicFacadeB other)
    {
        public CyclicFacadeB Other { get; } = other;
    }

    [Simulation(Facade = true)]
    private class CyclicFacadeB(CyclicFacadeA other)
    {
        public CyclicFacadeA Other { get; } = other;
    }

    // Stands for a Presentation with only event handlers: nothing takes it in a constructor
    [Presentation]
    private class UnrequestedPresentation
    {
        public static int Created;

        public UnrequestedPresentation() => Created++;
    }
}
