using Godot;
using GodotBox.Godot.Nodes;

namespace NeonWarfare.GameTests.GodotBox.Fixtures;

/// <summary>
/// One member of every kind <see cref="AbstractStorage"/> has to tell apart: only exported properties are
/// registered, and the game's storages declare their scenes exactly that way.
/// </summary>
public partial class SceneStorageFixture : AbstractStorage
{
    [Export] public PackedScene? First { get; set; }

    [Export] public PackedScene? Second { get; set; }

    [Export] public PackedScene? AssignedInPreReady { get; set; }

    public PackedScene? NotExported { get; set; }

    [Export] public PackedScene? ExportedField;

    public override void _PreReady()
    {
        AssignedInPreReady = NotExported;
    }
}
