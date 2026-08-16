using Godot;
using GodotBox;

namespace NeonWarfare.Scenes.Root.Starters;

public record RootData(
    NodeContainer MainSceneContainer,
    NodeContainer LoadingScreenContainer,
    RootPackedScenes PackedScenes,
    SceneTree SceneTree);