using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.Misc;

namespace NeonWarfare.Scenes.World.Scenes;

public partial class PackedScenes : GameCheckedAbstractStorage
{
    [ExportGroup("Entities")]
    [Export] [NotNull] public PackedScene Character { get; private set; }
    [Export] [NotNull] public PackedScene Wall { get; private set; }
}