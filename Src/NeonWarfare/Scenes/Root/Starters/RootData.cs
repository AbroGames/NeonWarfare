using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;

namespace NeonWarfare.Scenes.Root.Starters;

public record RootData(
    NodeContainer MainSceneContainer,
    NodeContainer LoadingScreenContainer,
    RootPackedScenes PackedScenes,
    SceneTree SceneTree);