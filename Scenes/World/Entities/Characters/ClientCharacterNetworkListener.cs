using System;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;

namespace NeonWarfare.Scenes.World.Entities.Characters;

public partial class ClientCharacter 
{
    private float LabelSpawnPositionDeviation { get; set; } = 10f;
    
    // Кажется, логике этой штуки место в скилле. У разных скиллов может быть разная логика отрисовки эффекта. Надо подумац.
    [EventListener(ListenerSide.Client)]
    public void OnDamageCharacterPacket(SC_DamageCharacterPacket damageCharacterPacket)
    {
        Skill skill = SkillStorage.GetSkill(damageCharacterPacket.SkillType); //TODO Я полагаю в Skill надо добавить ссылку (лямбду) на PackedScene с эффектом урона (брызги крови). Как сейчас сделано с Action для Skill.
        //Если дамаг нанес данный клиент
        if (damageCharacterPacket.AuthorPeerId == ClientRoot.Instance.Game.PlayerProfile.PeerId)
        {
            var label = Fx.CreateFloatingLabel($"{damageCharacterPacket.Damage:N0}", Colors.Red, 1);
            label.Position = Rand.InsideCircle(new (Position, LabelSpawnPositionDeviation));
            ClientRoot.Instance.Game.World.AddChild(label);
        }
        
        //Если дамаг нанес враг
        if (damageCharacterPacket.AuthorPeerId == -1)
        {
            //TODO я полагаю в этой ситуации мы индикатор урона не рисуем
        }
        
        ProcessDamage(damageCharacterPacket);
    }

    protected virtual void ProcessDamage(SC_DamageCharacterPacket damageCharacterPacket) {}
    
    [EventListener(ListenerSide.Client)]
    public void OnHealCharacterPacket(SC_HealCharacterPacket healCharacterPacket)
    {
        var skillFx = Fx.CreateHealFx();
        skillFx.UseGlobalRotation(0);
        skillFx.Target = this;
        skillFx.Position = Position;
        ClientRoot.Instance.Game.World.AddChild(skillFx);
    }
    
    [EventListener(ListenerSide.Client)]
    public void OnResurrectCharacterPacket(SC_ResurrectCharacterPacket resurrectCharacterPacket)
    {
        var skillFx = Fx.CreateResurrectFx();
        skillFx.UseGlobalRotation(0);
        skillFx.Target = this;
        skillFx.Position = Position;
        ClientRoot.Instance.Game.World.AddChild(skillFx);
    }
}
