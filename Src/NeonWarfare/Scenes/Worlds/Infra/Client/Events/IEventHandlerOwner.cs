namespace NeonWarfare.Scenes.Worlds.Infra.Client.Events;

/// <summary>
/// Marks a Presentation that declares <see cref="EventHandlerAttribute"/> methods.
/// Empty, because <see cref="EventDispatcher"/> finds the handlers by reflection. The marker only puts the owner into
/// <c>IEnumerable&lt;IEventHandlerOwner&gt;</c>.
/// </summary>
public interface IEventHandlerOwner;
