using Godot;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Features.Players;

public class PlayerModel
{
    // Immutable after construction so that PersistenceModel can keep PlayerByUid[uid].Uid == uid
    [field: Replicated] public string Uid { get; private set; }
    [Replicated] public string Nick;
    [Replicated] public Color Color;
    [Replicated] public bool IsAdmin;

    private PlayerModel() { }

    public PlayerModel(string uid)
    {
        Uid = uid;
    }
}