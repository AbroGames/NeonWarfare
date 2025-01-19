using Godot;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayer : ClientAlly 
{
    
    public ClientPlayerProfile PlayerProfile { get; private set; }
    private ManualCooldown _shootCooldown = new(0.5);
    
    public void InitOnProfile(ClientPlayerProfile playerProfile)
    {
        base.InitOnProfile(playerProfile);
        PlayerProfile = playerProfile;
    }

    //TODO del after test
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        _shootCooldown.Update(delta);

        if (Input.IsActionPressed(Keys.AttackPrimary) && _shootCooldown.IsCompleted)
        {
            _shootCooldown.Restart();
            Network.SendToServer(new ServerPlayer.CS_PlayerWantShootPacket(Position + Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * 40, Rotation));
        }
    }
}
