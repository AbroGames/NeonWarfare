using System;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;

namespace NeonWarfare.Scenes.World.Entities.Characters;

public partial class ClientCharacter 
{
    [EventListener(ListenerSide.Client)]
    public void OnDamageCharacterPacket(CS_DamageCharacterPacket damageCharacterPacket)
    {
        Skill skill = SkillStorage.GetSkill(damageCharacterPacket.SkillType); //TODO Я полагаю в Skill надо добавить ссылку (лямбду) на PackedScene с эффектом урона (брызги крови). Как сейчас сделано с Action для Skill.
        //Если дамаг нанес данный клиент
        if (damageCharacterPacket.AuthorPeerId == ClientRoot.Instance.Game.PlayerProfile.PeerId)
        {
            //TODO рисуем сколько урона нанес игрок
        }
        //Если дамаг нанес враг
        if (damageCharacterPacket.AuthorPeerId == -1)
        {
            //TODO я полагаю в этой ситуации мы индикатор урона не рисуем
        }
        //Если дамаг нанесли по данному клиенту
        if (this is ClientPlayer) //TODO вместо проверки я бы просто вызвал виртуальный метод, который переопределен в ClientPlayer
        {
            //TODO рисуем, сколько урона получил игрок
            //TODO учти, что это условие может сработать одновременно вместе с одним из верхних, если враг нанес урон игроку или игрок сам себе
        }
    }
    
    [EventListener(ListenerSide.Client)]
    public void OnHealCharacterPacket(CS_HealCharacterPacket healCharacterPacket)
    {
        
    }
    
    [EventListener(ListenerSide.Client)]
    public void OnResurrectCharacterPacket(CS_ResurrectCharacterPacket resurrectCharacterPacket)
    {
        
    }
}
