namespace NeonWarfare.Scenes.OldWorld.WorldServices.DataSerializer;

public interface ISerializableStorage
{
    
    public byte[] SerializeStorage();
    public void DeserializeStorage(byte[] storageBytes);
    public void SetAllPropertyListeners();
}