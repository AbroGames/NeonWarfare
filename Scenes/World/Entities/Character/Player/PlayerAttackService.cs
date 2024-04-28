using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class PlayerAttackService
{
    
    /*
     * ОСНОВНАЯ АТАКА
     */
    
    [EventListener(ListenerSide.Server)]
    public void OnClientPlayerPrimaryAttackPacket(ClientPlayerPrimaryAttackPacket clientPlayerPrimaryAttackPacket)
    {
        var player = Root.Instance.Server.PlayerServerInfo[clientPlayerPrimaryAttackPacket.Sender.Id].Player;
		
        // Создание снаряда
        Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
        // Установка начальной позиции снаряда
        bullet.GlobalPosition = Vec(clientPlayerPrimaryAttackPacket.X, clientPlayerPrimaryAttackPacket.Y);
        // Установка направления движения снаряда
        bullet.Rotation = clientPlayerPrimaryAttackPacket.Dir;
        bullet.Author = Bullet.AuthorEnum.PLAYER;
        bullet.Speed *= 3;
        bullet.RemainingDamage = player.PrimaryDamage;
        bullet.RemainingDistance = player.PrimaryDistance;
        bullet.Scale *= 2;
        bullet.Source = player;
        player.GetParent().AddChild(bullet);
        long nid = Root.Instance.NetworkEntityManager.AddEntity(bullet);
        
        Network.SendPacketToClients(new ServerPlayerPrimaryAttackPacket(nid, bullet.Position.X, bullet.Position.Y, bullet.Rotation, bullet.Speed));
    }
    
    [EventListener(ListenerSide.Client)]
    public void OnServerPlayerPrimaryAttackPacket(ServerPlayerPrimaryAttackPacket serverPlayerPrimaryAttackPacket)
    {
        
        // Создание снаряда
        Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
        bullet.Position = Vec(serverPlayerPrimaryAttackPacket.X, serverPlayerPrimaryAttackPacket.Y);
        bullet.Rotation = serverPlayerPrimaryAttackPacket.Dir;
        bullet.Scale *= 2;
        bullet.Speed = serverPlayerPrimaryAttackPacket.MovementSpeed;
        bullet.Author = Bullet.AuthorEnum.PLAYER;

        Root.Instance.CurrentWorld.AddChild(bullet);
        Root.Instance.NetworkEntityManager.AddEntity(bullet, serverPlayerPrimaryAttackPacket.Nid);
        
        Audio2D.PlaySoundAt(Sfx.SmallLaserShot, bullet.Position, 1f).PitchVariation(0.05f);
    }
    
    /*
     * ДОПОЛНИТЕЛЬНАЯ АТАКА
     */
    [EventListener(ListenerSide.Server)]
    public void OnClientPlayerSecondaryAttackPacket(ClientPlayerSecondaryAttackPacket clientPlayerSecondaryAttackPacket)
    {
        var player = Root.Instance.Server.PlayerServerInfo[clientPlayerSecondaryAttackPacket.Sender.Id].Player;
		
        var bulletsCount = 5;
        var spread = Mathf.DegToRad(18);
        var speedSpread = 0.1;
		
        for (int i = 0; i < bulletsCount; i++)
        {
            // Создание снаряда
            Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
            // Установка начальной позиции снаряда
            bullet.GlobalPosition = Vec(clientPlayerSecondaryAttackPacket.X, clientPlayerSecondaryAttackPacket.Y);
            // Установка направления движения снаряда
            bullet.Rotation = clientPlayerSecondaryAttackPacket.Dir + Rand.Range(-spread, spread);
            bullet.Author = Bullet.AuthorEnum.PLAYER;
            bullet.Speed = bullet.Speed * 2 + Rand.Range(-bullet.Speed * speedSpread, bullet.Speed * speedSpread);
            bullet.RemainingDistance = player.SecondaryDistance;
            bullet.RemainingDamage = player.SecondaryDamage;
            bullet.Source = player;
            player.GetParent().AddChild(bullet);
            long nid = Root.Instance.NetworkEntityManager.AddEntity(bullet);
            
            Network.SendPacketToClients(new ServerPlayerSecondaryAttackPacket(nid, bullet.Position.X, bullet.Position.Y, bullet.Rotation, bullet.Speed));
        }
    }

    [EventListener(ListenerSide.Client)]
    public void OnServerPlayerSecondaryAttackPacket(ServerPlayerSecondaryAttackPacket serverPlayerSecondaryAttackPacket)
    {
        
        // Создание снаряда
        Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
        bullet.Position = Vec(serverPlayerSecondaryAttackPacket.X, serverPlayerSecondaryAttackPacket.Y);
        bullet.Rotation = serverPlayerSecondaryAttackPacket.Dir;
        bullet.Speed = serverPlayerSecondaryAttackPacket.MovementSpeed;
        bullet.Author = Bullet.AuthorEnum.PLAYER;
        
        Root.Instance.CurrentWorld.AddChild(bullet);
        Root.Instance.NetworkEntityManager.AddEntity(bullet, serverPlayerSecondaryAttackPacket.Nid);
        
        Audio2D.PlaySoundAt(Sfx.SmallLaserShot, bullet.Position, 0.5f);
    }
}