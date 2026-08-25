namespace NeonWarfare.Scripts.GlobalServices;

public class TerminationSignalsService : KludgeBox.Godot.Services.TerminationSignalsService
{

    // Runs on a runtime thread, not the main one — MainScene.Shutdown() only queues a deferred call.
    // Goes through MainScene, so Init() must be called only after MainScene.Init().
    protected override void Shutdown()
    {
        Services.MainScene.Shutdown();
    }
}
