using Godot;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Networking;
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
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        //TODO del after test
        if (Input.IsActionJustReleased(Keys.AttackPrimary))
        {
            Network.SendToServer(new ServerPlayer.CS_UseSkillPacket(0, Position, Rotation, GetGlobalMousePosition()));
        }
        if (Input.IsActionJustReleased(Keys.AttackSecondary))
        {
            Network.SendToServer(new ServerPlayer.CS_UseSkillPacket(1, Position, Rotation, GetGlobalMousePosition()));
        }
        
        /*TODO 
        _shootCooldown.Update(delta);

        if (Input.IsActionPressed(Keys.AttackPrimary) && _shootCooldown.IsCompleted)
        {
            _shootCooldown.Restart();
            Network.SendToServer(new ServerPlayer.CS_PlayerWantShootPacket(Position + Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * 40, Rotation));
        }*/
    }
}
