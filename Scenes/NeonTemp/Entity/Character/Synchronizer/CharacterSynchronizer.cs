using Godot;
using KludgeBox.DI.Requests.ParentInjection;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer : Node
{
    [Parent] private Character _character;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        StatusEffects_OnReady();
        Stats_OnReady();
        Controller_OnReady();
    }
}