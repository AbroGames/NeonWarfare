using Godot;
using NeonWarfare.Scenes.Game.ClientGame.AllyProfile;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientAlly : ClientCharacter 
{
    [Export] [NotNull] public Sprite2D ShieldSprite { get; private set; }
    
    public ClientAllyProfile AllyProfile { get; private set; }
    
    public void InitOnProfile(ClientAllyProfile allyProfile)
    {
        AllyProfile = allyProfile;
        allyProfile.Ally = this;
    }
}
