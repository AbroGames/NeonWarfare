using System.IO;

namespace KludgeBox.Networking;

public interface IBinaryStreamSerializable
{
    /// <summary>
    /// Writes its binary representation to a <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="writer"></param>
    void Serialize(BinaryWriter writer);
    
    /// <summary>
    /// Reads data from <see cref="BinaryReader"/> to existing (this) instance.
    /// </summary>
    /// <param name="reader"></param>
    void Deserialize(BinaryReader reader);
}