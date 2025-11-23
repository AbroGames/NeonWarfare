using Godot;
using MessagePack;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Data.General;

public partial class GeneralDataStorage : Node, ISerializableStorage
{

    [MessagePackObject]
    public record InnerStorage
    {
        [Key(0)] public GeneralData GeneralData = new();
    }

    public GeneralData GeneralData => _innerStorage.GeneralData;
    private InnerStorage _innerStorage = new();

    public GeneralDataStorage()
    {
        AddGeneralData(new GeneralData());
    }

    private void AddGeneralData(GeneralData general)
    {
        _innerStorage.GeneralData = general;
        SetPropertyListener(_innerStorage.GeneralData);
    }

    private void SetPropertyListener(GeneralData general)
    {
        general.PropertyChanged += (g, _) => UpdateGeneral((GeneralData) g);
    }

    private void UpdateGeneral(GeneralData general) => Rpc(MethodName.UpdateGeneralRpc, Serialize(general));
    [Rpc(CallLocal = false)]
    private void UpdateGeneralRpc(byte[] generalBytes)
    {
        GeneralData general = Deserialize<GeneralData>(generalBytes);
        AddGeneralData(general);
    }

    public byte[] SerializeStorage()
    {
        return Serialize(_innerStorage);
    }

    public void DeserializeStorage(byte[] storageBytes)
    {
        _innerStorage = Deserialize<InnerStorage>(storageBytes);
    }

    public void SetAllPropertyListeners()
    {
        SetPropertyListener(_innerStorage.GeneralData);
    }
}