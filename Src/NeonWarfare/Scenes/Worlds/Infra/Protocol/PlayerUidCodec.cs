using RepliCAT.Bits;
using RepliCAT.Codecs;

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// Replicates a <see cref="PlayerUid"/> as its string, so a <c>default</c> uid travels as <c>null</c>.
/// </summary>
public sealed class PlayerUidCodec : IReplicationCodec<PlayerUid>
{
    public static readonly PlayerUidCodec Instance = new();

    public void Write(BitWriter writer, PlayerUid value) => StringCodec.Default.Write(writer, value.Value);

    public PlayerUid Read(ref BitReader reader) => new(StringCodec.Default.Read(ref reader));

    public bool IsChanged(PlayerUid lastSent, PlayerUid current) =>
        StringCodec.Default.IsChanged(lastSent.Value, current.Value);
}
