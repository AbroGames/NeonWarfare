using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>
/// Variant B: the bots are RigidBody2D, the player is a RigidBody2D or, as a tuning option, a
/// CharacterBody2D. This is the only IPhysicsVariant implementation on this branch; the harness
/// discovers it by type.
/// </summary>
public sealed class RigidBodyVariant : IPhysicsVariant
{
    private const float HeavyPlayerMass = 50f;

    private static readonly float[] Retentions = [0.5f, 0.25f, 0f];

    private PlayerKind _playerKind = PlayerKind.Rigid;
    private int _retentionIndex;

    public string Title => "B - RigidBody2D";

    public string FileStamp => "rigidbody";

    public string TuningText =>
        $"K retention {RigidBodyBenchBody.SolverResponseRetention:0.##} | M player {_playerKind}";

    public IBenchPlayerBody CreatePlayer() => _playerKind switch
    {
        PlayerKind.Heavy => new RigidBodyBenchPlayer(HeavyPlayerMass),
        PlayerKind.Character => new CharacterBodyPlayer(),
        _ => new RigidBodyBenchPlayer(1f),
    };

    public IBenchBotBody CreateBot() => new RigidBodyBenchBot();

    public TuningKeyResult HandleTuningKey(Key key)
    {
        switch (key)
        {
            case Key.K:
                _retentionIndex = (_retentionIndex + 1) % Retentions.Length;
                RigidBodyBenchBody.SolverResponseRetention = Retentions[_retentionIndex];
                return TuningKeyResult.Handled;
            case Key.M:
                _playerKind = (PlayerKind)(((int)_playerKind + 1) % 3);
                return TuningKeyResult.RecreatePlayer;
            default:
                return TuningKeyResult.Ignored;
        }
    }

    private enum PlayerKind
    {
        Rigid,
        Heavy,
        Character,
    }
}
