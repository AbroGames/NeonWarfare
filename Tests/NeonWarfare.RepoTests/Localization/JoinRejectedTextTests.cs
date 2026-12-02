using System.Text.RegularExpressions;
using Mono.Cecil;
using NeonWarfare.RepoTests.Architecture;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Localization;

/// <summary>
/// The rejection of a nick tells the player the rule it broke, so the numbers in the text are a copy of the
/// server's constants: a change to the rule would otherwise leave every translation quietly wrong.
/// </summary>
[Collection(GameAssembly.Collection)]
public partial class JoinRejectedTextTests
{
    private const string InvalidNickKey = "MESSAGE_MENU__JOIN_REJECTED_INVALID_NICK";
    private const string JoinRequestHandler = WorldLayers.WorldNamespace + ".Features.Players.JoinRequestHandler";
    private const string NickMinLength = "NickMinLength";
    private const string NickMaxLength = "NickMaxLength";

    [Theory]
    [MemberData(nameof(FileSources.Translations), MemberType = typeof(FileSources))]
    public void InvalidNickText_StatesTheNickLengthLimits(string relativePath)
    {
        PoFile file = PoFile.Load(RepositoryPaths.Absolute(relativePath));
        PoEntry entry = file.Entries.Single(entry => entry.Key == InvalidNickKey);
        TypeDefinition handler = GameAssembly.Instance.FindByName(JoinRequestHandler)
                                 ?? throw new InvalidOperationException($"{JoinRequestHandler} is not found");

        int[] expected = [Constant(handler, NickMinLength), Constant(handler, NickMaxLength)];
        int[] actual = Number().Matches(entry.Translation).Select(match => int.Parse(match.Value)).ToArray();

        Assert.True(
            actual.SequenceEqual(expected),
            $"{relativePath}:{entry.Line}: '{entry.Translation}' states {string.Join(", ", actual)}, " +
            $"but {GameAssembly.ShortName(handler)} allows a nick of {expected[0]} to {expected[1]} characters");
    }

    private static int Constant(TypeDefinition type, string name)
    {
        FieldDefinition field = type.Fields.SingleOrDefault(field => field.Name == name && field.HasConstant)
                                ?? throw new InvalidOperationException(
                                    $"{GameAssembly.ShortName(type)}::{name} is not a constant any more");
        return (int) field.Constant;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex Number();
}
