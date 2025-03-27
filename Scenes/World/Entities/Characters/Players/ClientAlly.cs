using Godot;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientAlly : ClientCharacter 
{
    [Export] [NotNull] public Sprite2D ShieldSprite { get; private set; }
    
    public ClientAllyProfile AllyProfile { get; private set; }
    public string Name { get; private set; }
    
    public void InitOnProfile(ClientAllyProfile allyProfile)
    {
        AllyProfile = allyProfile;

        Name = allyProfile.Name;
        
        Color = allyProfile.Color;
        MaxHp = allyProfile.MaxHp;
        Hp = MaxHp;
        RegenHpSpeed = allyProfile.RegenHpSpeed;
        MovementSpeed = allyProfile.MovementSpeed;
        RotationSpeed = allyProfile.RotationSpeed;
    }

    public override void InitOnSpawnPacket(Vector2 position, float rotation, Color color)
    {
        base.InitOnSpawnPacket(position, rotation, color);
        
        if (color != new Color(0, 0, 0, 0))
        {
            ShieldSprite.Modulate = new Color(color.R, color.G, color.B, ShieldSprite.Modulate.A);
        }
    }

}
