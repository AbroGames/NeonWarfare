using Godot;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

public class PlayerModel
{
    /// <summary>
    /// Never changes after construction, so <c>PlayersModel</c> can keep <c>PlayerByUid[uid].Uid == uid</c>.
    /// </summary>
    [field: Replicated] public PlayerUid Uid { get; private set; }
    [Replicated] public string Nick;
    [Replicated] public Color Color;
    [Replicated] public bool IsAdmin;

    private PlayerModel() { }

    public PlayerModel(PlayerUid uid)
    {
        Uid = uid;
    }
}