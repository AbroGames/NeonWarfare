using System.Collections.Generic;

namespace NeonWarfare.Scenes.World.Services.StartStop;

public interface IWorldTreeLoader
{
    public string GetName();

    public void Create(World world) { }
    public void Init(World world) { }
    public void Finish(World world) { }

    public List<string> GetCreateRequirements() { return []; }
    public List<string> GetInitRequirements() { return []; }
    public List<string> GetFinishRequirements() { return []; }
}