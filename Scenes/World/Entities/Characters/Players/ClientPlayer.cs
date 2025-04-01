using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayer : ClientAlly 
{
    
    public ClientPlayerProfile PlayerProfile { get; private set; }

    public record ClientSkillInfo(ManualCooldown Cooldown, StringName ActionToActivate);
    private Dictionary<long, ClientSkillInfo> _skillCooldownById = new();

    public void InitComponents()
    {
        AddChild(new ClientPlayerMovementComponent());
        
        RotateComponent rotateComponent = new RotateComponent();
        rotateComponent.GetTargetGlobalPositionFunc = () => GetGlobalMousePosition();
        rotateComponent.GetRotationSpeedFunc = () => IsDead ? 0 : RotationSpeed;
        AddChild(rotateComponent);
    }
    
    public void InitOnProfile(ClientPlayerProfile playerProfile)
    {
        base.InitOnProfile(playerProfile);
        
        foreach (var kv in playerProfile.SkillById)
        {
            _skillCooldownById.Add(kv.Key, new ClientSkillInfo(new ManualCooldown(kv.Value.Cooldown, true), kv.Value.ActionToActivate));
        }
        PlayerProfile = playerProfile;
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);

        double skillCooldownFactorWhileDead = ClientRoot.Instance.Game.GameSettings.SkillCooldownFactorWhileDead;
        foreach (var kv in _skillCooldownById)
        {
            kv.Value.Cooldown.Update(IsDead ? delta*skillCooldownFactorWhileDead : delta);  //Если персонаж мертв, то скиллы откатываются медленней
            if (Input.IsActionPressed(kv.Value.ActionToActivate) && kv.Value.Cooldown.IsCompleted && !IsDead)
            {
                kv.Value.Cooldown.Restart();
                Network.SendToServer(new ServerPlayer.CS_UseSkillPacket(kv.Key, Position, Rotation, GetGlobalMousePosition()));
            }
        }
    }
}
