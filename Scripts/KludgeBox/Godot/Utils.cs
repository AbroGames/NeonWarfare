using Godot;

namespace NeonWarfare.Scripts.KludgeBox.Godot;

public static partial class Utils 
{
    public static SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();
}
