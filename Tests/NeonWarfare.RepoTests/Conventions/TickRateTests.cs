using Mono.Cecil;
using NeonWarfare.RepoTests.Architecture;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Conventions;

/// <summary>
/// The game converts seconds to ticks through a constant, so it stays pure and works outside the engine. The engine
/// runs the ticks at the rate of project.godot, and a change of that rate in the editor would leave every cooldown and
/// duration quietly off by the ratio.
/// </summary>
[Collection(GameAssembly.Collection)]
public class TickRateTests
{
    private const string TickRate = WorldLayers.WorldNamespace + ".Infra.Tick.TickRate";
    private const string TicksPerSecond = "TicksPerSecond";
    private const string PhysicsTicksPerSecond = "physics/common/physics_ticks_per_second";
    private const int EngineDefault = 60;

    [Fact]
    public void TicksPerSecond_IsTheEnginePhysicsRate()
    {
        TypeDefinition type = GameAssembly.Instance.FindByName(TickRate)
                              ?? throw new InvalidOperationException($"{TickRate} is not found");
        FieldDefinition field =
            type.Fields.SingleOrDefault(field => field.Name == TicksPerSecond && field.HasConstant)
            ?? throw new InvalidOperationException($"{TickRate}::{TicksPerSecond} is not a constant");
        string? setting = GodotProjectFile.Current.Setting(PhysicsTicksPerSecond);
        int engine = setting == null ? EngineDefault : int.Parse(setting);

        Assert.True(
            (int) field.Constant == engine,
            $"{GameAssembly.ShortName(type)}.{TicksPerSecond} is {field.Constant}, but {PhysicsTicksPerSecond} " +
            $"of project.godot is {engine}");
    }
}
