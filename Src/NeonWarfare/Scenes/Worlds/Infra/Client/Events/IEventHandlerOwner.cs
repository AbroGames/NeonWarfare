namespace NeonWarfare.Scenes.Worlds.Infra.Client.Events;

/// <summary>
/// A Presentation that declares <see cref="EventHandlerAttribute"/> methods. Empty: <see cref="EventDispatcher"/>
/// finds the handlers by reflection and calls nothing through a typed contract, so the marker only puts the owner
/// into <c>IEnumerable&lt;IEventHandlerOwner&gt;</c>.
/// </summary>
public interface IEventHandlerOwner;
