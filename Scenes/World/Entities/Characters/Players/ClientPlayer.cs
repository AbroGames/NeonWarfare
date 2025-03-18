using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scripts.Utils.Components;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayer : ClientAlly 
{
    
    public ClientPlayerProfile PlayerProfile { get; private set; }

    public void InitComponents()
    {
        AddChild(new ClientPlayerMovementComponent());
        
        RotateComponent rotateComponent = new RotateComponent();
        rotateComponent.GetTargetGlobalPositionFunc = () => GetGlobalMousePosition();
        rotateComponent.GetRotationSpeedFunc = () => RotationSpeed;
        AddChild(rotateComponent);
    }
    
    public void InitOnProfile(ClientPlayerProfile playerProfile)
    {
        base.InitOnProfile(playerProfile);
        PlayerProfile = playerProfile;
    }
}
