using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.Camera;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld : Node2D
{
    public WorldEnvironment Environment {get; private set;}
    public Background Background {get; private set;}
    public Camera Camera {get; private set;}
    
    // achievements
    public int EnemiesKilled;
    
    public double TimeSinceLastResurrection;
    public long LastResurrectedTargetId;
    
    
    public override void _EnterTree()
    {
        Environment = ClientRoot.Instance.PackedScenes.WorldEnvironment.Instantiate<WorldEnvironment>();
        AddChild(Environment);
        
        Background = ClientRoot.Instance.PackedScenes.Background.Instantiate<Background>();
        AddChild(Background);
        
        Camera = new Camera();
        AddChild(Camera);
    }

    public override void _Process(double delta)
    {
        if(!Player.IsValid())
            return;

        var distance = Player.Position.Length();
        if (distance > AchievementsPrerequisites.WhereAmI_Distance)
        {
            ClientRoot.Instance.UnlockAchievement(AchievementIds.CrowdAchievement);
        }
        
        TimeSinceLastResurrection += delta;
    }
}
