using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Events;
namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientAllyProfile
{
    
    public void OnChangeAllyProfilePacket(SC_ChangeAllyProfilePacket changeAllyProfilePacket)
    {
        MaxHp = changeAllyProfilePacket.MaxHp;
        RegenHpSpeed = changeAllyProfilePacket.RegenHpSpeed;
        MovementSpeed = changeAllyProfilePacket.MovementSpeed;
        RotationSpeed = changeAllyProfilePacket.RotationSpeed;
    }
    
    /*
     * Изменяем характеристики игрока или союзника
     */
    [EventListener(ListenerSide.Client)]
    public static void OnChangeAllyProfilePacketListener(SC_ChangeAllyProfilePacket changeAllyProfilePacket)
    {
        ClientRoot.Instance.Game.AllyProfilesByPeerId[changeAllyProfilePacket.PeerId].OnChangeAllyProfilePacket(changeAllyProfilePacket);
    }
}
