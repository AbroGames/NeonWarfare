using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Prototypes.CrowdPhysics;
using NeonWarfare.Scripts.Content.CmdArgs;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

/// <summary>
/// Runs the crowd physics prototype instead of the game: base init only, then the benchmark root goes
/// into the main scene container. No menu, no Game, no World, no network.
/// </summary>
public class PhysicsBenchmarkRootStarter : BaseRootStarter
{

    [Logger] private ILogger _log;

    public override void Init(RootData rootData)
    {
        base.Init(rootData);
        _log.Information("Initializing Physics benchmark...");
    }

    public override void Start(RootData rootData)
    {
        base.Start(rootData);
        _log.Information("Starting Physics benchmark...");

        PhysicsBenchmarkArgs args = PhysicsBenchmarkArgs.GetFromCmd(CmdArgsService);

        PhysicsBenchmarkRoot benchmarkRoot = new();
        benchmarkRoot.InitBenchArgs(args.BenchCount ?? 100, args.BenchAuto);
        rootData.MainSceneContainer.ChangeStoredNode(benchmarkRoot);
        Services.LoadingScreen.Clear();
    }
}
