using Mono.Cecil;
using Mono.Cecil.Cil;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Architecture;

/// <summary>
/// The server tick sends at the end of the physics step, so its node must run after every other one: a node with
/// the same priority could run after it, and whatever it changed would miss the tick's packet.
/// </summary>
[Collection(GameAssembly.Collection)]
public class TickPriorityTests
{
    private const string TickNode = WorldLayers.WorldNamespace + ".Composition.ServerTickNode";
    private const string PrioritySetter = "set_ProcessPhysicsPriority";
    private const string ScenePriority = "process_physics_priority";

    [Fact]
    public void MaxPhysicsPriority_IsSetOnlyByTheTickNode()
    {
        FailureReport report = new("int.MaxValue physics priority outside ServerTickNode");
        bool setByTickNode = false;

        foreach (TypeDefinition type in GameAssembly.Instance.Types)
        {
            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
            {
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is not MethodReference { Name: PrioritySetter }
                        || instruction.Previous is not { OpCode.Code: Code.Ldc_I4, Operand: int.MaxValue })
                    {
                        continue;
                    }

                    if (GameAssembly.SelfAndEnclosing(type).Any(owner => owner.FullName == TickNode))
                    {
                        setByTickNode = true;
                    }
                    else
                    {
                        report.Add($"{GameAssembly.Describe(method)}: sets the physics priority to int.MaxValue");
                    }
                }
            }
        }

        // Without it a rename of the node would leave the rule nothing to check
        Assert.True(setByTickNode, $"{TickNode} no longer sets its physics priority to int.MaxValue");
        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(FileSources.Scenes), MemberType = typeof(FileSources))]
    public void Scene_DoesNotUseMaxPhysicsPriority(string relativePath)
    {
        FailureReport report = new($"int.MaxValue physics priority in {relativePath}");
        string[] lines = TextFile.ReadLines(Path.Combine(RepositoryPaths.Root, relativePath));

        for (int i = 0; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts is [ScenePriority, var value] && value == int.MaxValue.ToString())
            {
                report.Add($"{relativePath}:{i + 1}: only ServerTickNode may run last in the physics step");
            }
        }

        report.AssertEmpty();
    }
}
