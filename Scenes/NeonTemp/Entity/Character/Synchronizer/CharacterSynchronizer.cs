using Godot;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer : Node
{
    public override void _Ready()
    {
        Di.Process(this);
    }

    public void InitPostReady(Character character)
    {
        StatusEffects_InitPostReady(character);
        Stats_InitPostReady(character);
        Controller_InitPostReady(character);
    }
}