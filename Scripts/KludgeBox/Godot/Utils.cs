using Godot;

namespace KludgeBox;

public static partial class Utils 
{
    public static SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();
}