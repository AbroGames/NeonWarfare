using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;
using AbstractStorage = KludgeBox.Godot.Nodes.AbstractStorage;

namespace Kludgeful.Main;

public abstract partial class CheckedAbstractStorage : AbstractStorage
{
    [Logger] ILogger _logger;
    public override void _PreReady()
    {
        Di.Process(this);
    }
}