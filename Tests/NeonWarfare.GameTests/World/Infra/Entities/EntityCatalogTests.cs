using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Features.Storages;
using NeonWarfare.Scenes.World.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.Entities;

[TestSuite]
public class EntityCatalogTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void KindIds_ScenesFirstInCatalogOrder_ThenNodeTypesInMappingOrder()
    {
        PackedScene first = TestWorldScenes.Pack(new Node());
        PackedScene second = TestWorldScenes.Pack(new Node());

        var catalog = new EntityCatalog(
            [first, second], [typeof(PersistenceModel), typeof(SessionStorage), typeof(PersistenceStorage)]);

        AssertThat(catalog.GetKindId(first)).IsEqual(0);
        AssertThat(catalog.GetKindId(second)).IsEqual(1);
        AssertThat(catalog.GetKindId(typeof(SessionStorage))).IsEqual(2);
        AssertThat(catalog.GetKindId(typeof(PersistenceStorage))).IsEqual(3);
        AssertThat(catalog.Descriptors).ContainsExactly(
            "scene " + first.ResourcePath, "scene " + second.ResourcePath,
            "type " + typeof(SessionStorage).FullName, "type " + typeof(PersistenceStorage).FullName);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Types_AbstractNonNodeAndGeneric_AreLeftOut()
    {
        var catalog = new EntityCatalog(
            [], [typeof(AbstractNode), typeof(PersistenceModel), typeof(GenericNode<>), typeof(SessionStorage)]);

        AssertThat(catalog.Descriptors).ContainsExactly("type " + typeof(SessionStorage).FullName);
        AssertThrown(() => catalog.GetKindId(typeof(AbstractNode))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId(typeof(PersistenceModel))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId(typeof(GenericNode<>))).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Create_TypeKind_IsANewInstanceNamedAfterItsType()
    {
        var catalog = new EntityCatalog([], [typeof(SessionStorage)]);
        int kind = catalog.GetKindId(typeof(SessionStorage));

        Node created = AutoFree(catalog.Create(kind))!;
        Node another = AutoFree(catalog.Create(kind))!;

        AssertThat(created).IsInstanceOf<SessionStorage>();
        AssertThat(created.Name.ToString()).IsEqual(nameof(SessionStorage));
        AssertThat(another).IsNotSame(created);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Create_SceneKind_InstantiatesTheScene()
    {
        var content = new Node2D { Name = "Content" };
        PackedScene scene = TestWorldScenes.Pack(content);
        var catalog = new EntityCatalog([scene], [typeof(SessionStorage)]);

        Node created = AutoFree(catalog.Create(catalog.GetKindId(scene)))!;

        AssertThat(created).IsInstanceOf<Node2D>();
        AssertThat(created.Name.ToString()).IsEqual("Content");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void UnknownSceneTypeOrId_Throws()
    {
        var catalog = new EntityCatalog([TestWorldScenes.Pack(new Node())], [typeof(SessionStorage)]);

        AssertThrown(() => catalog.GetKindId(TestWorldScenes.Pack(new Node()))).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId((PackedScene) null!)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => catalog.GetKindId(typeof(PersistenceStorage))).IsInstanceOf<ArgumentException>();
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

        Node persistence = AutoFree(client.Create(server.GetKindId(typeof(PersistenceStorage))))!;
        Node session = AutoFree(client.Create(server.GetKindId(typeof(SessionStorage))))!;

        AssertThat(persistence).IsInstanceOf<PersistenceStorage>();
        AssertThat(session).IsInstanceOf<SessionStorage>();
        AssertThat(client.Descriptors.Skip(1)).ContainsExactly(server.Descriptors.Skip(1));
    }
}

// An abstract type may still declare a public parameterless constructor
public abstract partial class AbstractNode : Node
{
    public AbstractNode() { }
}

// Stands for a generic node type: the catalog cannot create one without its type arguments
public partial class GenericNode<T> : Node;
