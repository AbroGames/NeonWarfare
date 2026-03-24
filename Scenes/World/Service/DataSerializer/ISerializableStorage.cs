namespace NeonWarfare.Scenes.World.Service.DataSerializer;

public interface ISerializableStorage
{
    
    public byte[] SerializeStorage();
    public void DeserializeStorage(byte[] storageBytes);
    public void SetAllPropertyListeners();
}