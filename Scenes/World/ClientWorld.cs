using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.Utils.Camera;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld : Node2D
{

    public Camera Camera {get; private set;} = new();
    
    public override void _EnterTree()
    {
        AddChild(ClientRoot.Instance.PackedScenes.Background.Instantiate());
        AddChild(Camera);
    }
}
