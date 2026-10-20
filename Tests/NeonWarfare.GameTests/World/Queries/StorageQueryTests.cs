using GdUnit4;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;
using NeonWarfare.Scenes.World.Queries;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Queries;

[TestSuite]
public class StorageQueryTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Model_IsTheModelOfTheStorage()
    {
        var registry = new EntityRegistry();
        PersistenceStorage persistence = AutoFree(new PersistenceStorage())!;
        SessionStorage session = AutoFree(new SessionStorage())!;
        registry.Register(new NetId(1), persistence);
        registry.Register(new NetId(2), session);

        AssertThat(new PersistenceStorageQuery(registry).Model).IsSame(persistence.Model);
        AssertThat(new SessionStorageQuery(registry).Model).IsSame(session.Model);
    }

    // The registry's own message, so a read before the spawn says why the storage is missing
    [TestCase]
    [RequireGodotRuntime]
    public void Model_BeforeTheSpawn_Throws()
    {
        var registry = new EntityRegistry();

        AssertThrown(() => _ = new PersistenceStorageQuery(registry).Model)
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one PersistenceStorage, found 0.");
        AssertThrown(() => _ = new SessionStorageQuery(registry).Model)
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one SessionStorage, found 0.");
    }
}
