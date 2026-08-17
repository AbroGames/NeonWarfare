namespace NeonWarfare.Scenes.World.WorldServices.DataSerializer;

public interface ISerializableStorage
{
    
    public byte[] SerializeStorage();
    public void DeserializeStorage(byte[] storageBytes);
    public void SetAllPropertyListeners();
}