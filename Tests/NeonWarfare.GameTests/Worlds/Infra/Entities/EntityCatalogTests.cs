using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Entities;

[TestSuite]
public class EntityCatalogTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void KindIds_ScenesFirstByResourcePath_ThenNodeTypesInMappingOrder()
    {
        PackedScene first = PackAt("res://EntityCatalogTests/a.tscn");
        PackedScene second = PackAt("res://EntityCatalogTests/b.tscn");

        var catalog = new EntityCatalog(
            [second, first], [typeof(PlayersModel), typeof(OnlinePlayersStorage), typeof(PlayersStorage)]);

        AssertThat(catalog.GetKindId(first)).IsEqual(0);
        AssertThat(catalog.GetKindId(second)).IsEqual(1);
        AssertThat(catalog.GetKindId(typeof(OnlinePlayersStorage))).IsEqual(2);
        AssertThat(catalog.GetKindId(typeof(PlayersStorage))).IsEqual(3);
        AssertThat(catalog.Descriptors).ContainsExactly(
            "scene " + first.ResourcePath, "scene " + second.ResourcePath,
            "type " + typeof(OnlinePlayersStorage).FullName, "type " + typeof(PlayersStorage).FullName);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Types_AbstractNonNodeAndGeneric_AreLeftOut()
    {
        var catalog = new EntityCatalog(
            [], [typeof(AbstractNode), typeof(PlayersModel), typeof(GenericNode<>), typeof(OnlinePlayersStorage)]);

        AssertThat(catalog.Descriptors).ContainsExactly("type " + typeof(OnlinePlayersStorage).FullName);
        AssertThrown(() => catalog.GetKindId(typeof(AbstractNode))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId(typeof(PlayersModel))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId(typeof(GenericNode<>))).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Create_TypeKind_IsANewInstanceNamedAfterItsType()
    {
        var catalog = new EntityCatalog([], [typeof(OnlinePlayersStorage)]);
        int kind = catalog.GetKindId(typeof(OnlinePlayersStorage));

        Node created = AutoFree(catalog.Create(kind))!;
        Node another = AutoFree(catalog.Create(kind))!;

        AssertThat(created).IsInstanceOf<OnlinePlayersStorage>();
        AssertThat(created.Name.ToString()).IsEqual(nameof(OnlinePlayersStorage));
        AssertThat(another).IsNotSame(created);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Create_SceneKind_InstantiatesTheScene()
    {
        var content = new Node2D { Name = "Content" };
        PackedScene scene = TestWorldScenes.Pack(content);
        var catalog = new EntityCatalog([scene], [typeof(OnlinePlayersStorage)]);

        Node created = AutoFree(catalog.Create(catalog.GetKindId(scene)))!;

        AssertThat(created).IsInstanceOf<Node2D>();
        AssertThat(created.Name.ToString()).IsEqual("Content");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void UnknownSceneTypeOrId_Throws()
    {
        var catalog = new EntityCatalog([TestWorldScenes.Pack(new Node())], [typeof(OnlinePlayersStorage)]);

        AssertThrown(() => catalog.GetKindId(TestWorldScenes.Pack(new Node()))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId((PackedScene) null!)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId(typeof(PlayersStorage))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId((Type) null!)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.Create(-1)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.Create(2)).IsInstanceOf<ArgumentException>();
    }

    // The client builds its catalog on its own, from its own build: the kind id the server sends must name the same
    // node type there
    [TestCase]
    [RequireGodotRuntime]
    public void KindId_FromTheServerCatalog_CreatesTheSameTypeInAnIndependentClientCatalog()
    {
        PackedScene serverScene = TestWorldScenes.Pack(new Node());
        PackedScene clientScene = TestWorldScenes.Pack(new Node());
        var server = new EntityCatalog([serverScene], NetMessageCodecTests.CreateMapping().Types);
        var client = new EntityCatalog([clientScene], NetMessageCodecTests.CreateMapping().Types);

        Node players = AutoFree(client.Create(server.GetKindId(typeof(PlayersStorage))))!;
        Node onlinePlayers = AutoFree(client.Create(server.GetKindId(typeof(OnlinePlayersStorage))))!;

        AssertThat(players).IsInstanceOf<PlayersStorage>();
        AssertThat(onlinePlayers).IsInstanceOf<OnlinePlayersStorage>();
        AssertThat(client.Descriptors.Skip(1)).ContainsExactly(server.Descriptors.Skip(1));
    }

    // A scene packed in memory has an empty path, so the ordering by path needs a path set by hand
    private static PackedScene PackAt(string path)
    {
        PackedScene scene = TestWorldScenes.Pack(new Node());
        scene.TakeOverPath(path);
        return scene;
    }
}

// An abstract type may still declare a public parameterless constructor
public abstract partial class AbstractNode : Node
{
    public AbstractNode() { }
}

// Stands for a generic node type: the catalog cannot create one without its type arguments
public partial class GenericNode<T> : Node;
