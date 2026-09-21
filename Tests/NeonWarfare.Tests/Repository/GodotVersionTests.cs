using System.Xml.Linq;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Repository;

/// <summary>
/// The Godot version is written twice: in the <c>Sdk</c> attribute of the game's .csproj and of the game
/// test project's. It cannot be written once. The Godot editor, opening a project, rewrites that attribute
/// to exactly <c>Godot.NET.Sdk/&lt;its own version&gt;</c> and replaces a project that has none with a
/// generated one (<c>ProjectUtils.UpgradeProjectIfNeeded</c>), so a version taken from a property, from
/// global.json or from an explicit SDK import would not survive the next opening. The editor upgrades
/// only the game, and nothing else notices the drift: MSBuild resolves an SDK once per build, so the
/// project evaluated first silently decides the version for both, leaving only warning MSB4240.
/// </summary>
public class GodotVersionTests
{
    private const string GodotSdkPrefix = "Godot.NET.Sdk/";

    [Fact]
    public void GameTestProject_UsesTheGameGodotVersion()
    {
        string game = GodotVersion(RepositoryPaths.GameProjectPath);
        string gameTests = GodotVersion(RepositoryPaths.GameTestProjectPath);

        Assert.True(
            string.Equals(game, gameTests, StringComparison.Ordinal),
            $"{RepositoryPaths.Relative(RepositoryPaths.GameTestProjectPath)} builds with {GodotSdkPrefix}" +
            $"{gameTests}, the game with {game}. The editor upgrades only the game's .csproj — change the " +
            "Sdk attribute of the game tests by hand.");
    }

    /// <summary>
    /// Read as XML rather than matched as text, so a version mentioned in a comment is not taken for the
    /// real one. Anything but <c>Godot.NET.Sdk/&lt;version&gt;</c> throws: the editor writes only that form.
    /// </summary>
    private static string GodotVersion(string projectPath)
    {
        string? sdk = XDocument.Load(projectPath).Root?.Attribute("Sdk")?.Value;

        if (sdk is null
            || !sdk.StartsWith(GodotSdkPrefix, StringComparison.Ordinal)
            || sdk.Length == GodotSdkPrefix.Length)
        {
            throw new InvalidOperationException(
                $"{RepositoryPaths.Relative(projectPath)}: expected <Project Sdk=\"{GodotSdkPrefix}x.y.z\">, " +
                $"found Sdk=\"{sdk}\".");
        }

        return sdk[GodotSdkPrefix.Length..];
    }
}
