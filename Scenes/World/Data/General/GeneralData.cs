using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;

namespace NeonWarfare.Scenes.World.Data.General;

[MessagePackObject(AllowPrivate = true)]
public partial class GeneralData : ObservableObject
{
    [Key(0)] [ObservableProperty] private string _saveFileName; // Associated save file: world load from this file and will be to save to this file
}