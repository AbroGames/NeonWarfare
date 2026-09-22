using Godot;
using GodotBox.Godot.Nodes;
using KludgeBox.DI;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.GameTests.GodotBox.Fixtures;

/// <summary>
/// Its own injector rather than the game's: the check must come from whatever <see cref="GetDi"/> returns,
/// and the game's injector also carries the [Sync] scanner, which has nothing to do with storages.
/// </summary>
public partial class CheckedStorageFixture : CheckedAbstractStorage
{
    private static readonly DependencyInjector Injector = new();

    [Export] [NotNullStrict] public PackedScene? Required { get; set; }

    public override DependencyInjector GetDi() => Injector;
}
