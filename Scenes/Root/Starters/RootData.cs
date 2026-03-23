using Godot;
using NeonWarfare.Scenes.KludgeBox;

namespace NeonWarfare.Scenes.Root.Starters;

public record RootData(
    NodeContainer MainSceneContainer,
    NodeContainer LoadingScreenContainer,
    RootPackedScenes PackedScenes,
    SceneTree SceneTree);