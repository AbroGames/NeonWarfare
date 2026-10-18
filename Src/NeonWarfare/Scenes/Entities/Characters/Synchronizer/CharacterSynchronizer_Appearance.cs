using Godot;

namespace NeonWarfare.Scenes.Entities.Characters.Synchronizer;

public partial class CharacterSynchronizer
{

    private Sprite2D _sprite;
    
    private void Appearance_InitPostReady(Character character)
    {
        _sprite = character.Sprite;
    }

    public void Appearance_OnColorChanged(Color color) => Rpc(MethodName.Appearance_OnColorChangedRpc, color);
    [Rpc(CallLocal = true)]
    private void Appearance_OnColorChangedRpc(Color color)
    {
        if (!Net.IsClient()) return;
        _sprite.Modulate = color;
    }
}