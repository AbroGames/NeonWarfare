using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class CreateServerService
{

    [EventListener]
    public void OnCreateServerRequest(CreateServerRequest createServerRequest)
    {
        OS.CreateInstance(["--server", "--headless", "--port", createServerRequest.Port.ToString(), "--admin", createServerRequest.AdminNickname]);
    }
}