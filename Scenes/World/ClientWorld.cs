using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.Utils.Camera;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld : Node2D
{
    
    public WorldEnvironment Environment {get; private set;}
    public Background Background {get; private set;}
    public Camera Camera {get; private set;}
    
    public override void _EnterTree()
    {
        Environment = ClientRoot.Instance.PackedScenes.WorldEnvironment.Instantiate<WorldEnvironment>();
        AddChild(Environment);
        
        Background = ClientRoot.Instance.PackedScenes.Background.Instantiate<Background>();
        AddChild(Background);
        
        Camera = new Camera();
        AddChild(Camera);
    }
}
