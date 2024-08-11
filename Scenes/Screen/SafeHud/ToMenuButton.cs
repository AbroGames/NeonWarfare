using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ToMenuButton : Button
{
    
    public override void _Ready()
    {
        Pressed += () =>
        {
            //TODO по хорошему провести подготовительные действия, отправить на сервер сигнал, закрыть нормально подключение и т.п. Все это в отдельном методе, предположительно в ClientGameNetwork.cs
            ClientRoot.Instance.CreateMainMenu();
        };
    }

}