using System.Collections.Generic;

namespace NeonWarfare.Scenes.Game.ClientGame.ClientSettings;

public interface IOptionsProvider
{
    IReadOnlyList<string> GetOptions();
}