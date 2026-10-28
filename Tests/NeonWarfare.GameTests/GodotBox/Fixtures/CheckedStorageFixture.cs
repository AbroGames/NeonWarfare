using Godot;
using GodotBox.Godot.Nodes;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.GameTests.GodotBox.Fixtures;

public partial class CheckedStorageFixture : CheckedAbstractStorage
{
    [Export] [NotNullStrict] public PackedScene? Required { get; set; }
}
