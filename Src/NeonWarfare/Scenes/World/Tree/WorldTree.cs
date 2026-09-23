using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.World.Tree;

public partial class WorldTree : Node2D
{
    public Surface Surface { get; private set; }

    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }

    //TODO Убрать этот метод в сервис Simulation смены поверхности
    public void SetSurface(Surface surface)
    {
        if (surface == null) throw new ArgumentNullException(nameof(surface));
        if (surface == Surface)
        {
            _log.Warning("Attempt to set the already active surface {surfaceName}", surface.Name);
            return;
        }

        if (Surface != null)
        {
            // QueueFree alone keeps the old surface in the tree until the end of the frame,
            // next to the new one, so both would process this frame
            RemoveChild(Surface);
            Surface.QueueFree();
        }
        Surface = surface;
        AddChild(Surface);
    }
}