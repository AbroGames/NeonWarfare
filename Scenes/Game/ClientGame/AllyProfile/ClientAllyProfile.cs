using System;
using NeonWarfare;

public class ClientAllyProfile
{
    public long Id { get; private set; } //TODO и тут и на сервере и в Game/World переименовать Id на PeerId
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public ClientAlly Ally => ClientRoot.Instance.Game.World.AlliesById[Id];

    public ClientAllyProfile(long id)
    {
        Id = id;
    }
}