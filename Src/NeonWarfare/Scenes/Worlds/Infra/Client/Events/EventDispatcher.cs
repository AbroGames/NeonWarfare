using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Client.Events;

/// <summary>
/// Delivers a received events section to the <see cref="EventHandlerAttribute"/> methods of the Presentation.
/// </summary>
[Client]
public class EventDispatcher
{
    private record Handler(string Name, Action<Event> Call);

    private const BindingFlags HandlerFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly MethodInfo WrapMethod =
        typeof(EventDispatcher).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly ILogger _log = LogFactory.GetForStatic<EventDispatcher>();

    private readonly NetMessageCodec _codec;
    private readonly Dictionary<Type, List<Handler>> _handlersByType = new();
    private readonly IReadOnlySet<Type> _eventTypes;

    public EventDispatcher(NetMessageCodec codec, IEnumerable<IEventHandlerOwner> owners)
    {
        _codec = codec;
        HashSet<Type> eventTypes = codec.MappedTypes
            .Where(type => type.IsSubclassOf(typeof(Event)) && type is { IsNested: false, IsAbstract: false })
            .ToHashSet();

        foreach (IEventHandlerOwner owner in owners)
        {
            foreach (MethodInfo method in HandlerMethods(owner.GetType()))
            {
                string name = $"{method.DeclaringType!.Name}.{method.Name}";
                Type eventType = method.GetParameters()[0].ParameterType;
                if (!_handlersByType.TryGetValue(eventType, out List<Handler> handlers))
                {
                    handlers = [];
                    _handlersByType.Add(eventType, handlers);
                }
                handlers.Add(new Handler($"{name}({eventType.Name})", CreateCall(owner, method, eventType)));
            }
        }

        foreach (Type eventType in eventTypes.Where(type => !_handlersByType.ContainsKey(type)))
        {
            _log.Warning("{event} has no [EventHandler]: it will be read and dropped", eventType.Name);
        }
        _eventTypes = eventTypes;
    }

    /// <summary>
    /// Delivers an events section, received from the network or from the host's own World.
    /// The whole section is read before any handler runs, so a broken one changes nothing.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The section is broken.</exception>
    public void Dispatch(ReadOnlyMemory<byte> section)
    {
        IReadOnlyList<object> events = Read(section, out int bytesRead);
        if (bytesRead != section.Length)
        {
            throw new NetMessageFormatException(
                $"{bytesRead} of {section.Length} bytes read, a tick update carries one events section.");
        }
        Deliver(events);
    }

    private IReadOnlyList<object> Read(ReadOnlyMemory<byte> section, out int bytesRead)
    {
        return _codec.ReadSection(section, _eventTypes, out bytesRead);
    }

    private void Deliver(IReadOnlyList<object> events)
    {
        foreach (Event @event in events.Cast<Event>())
        {
            if (!_handlersByType.TryGetValue(@event.GetType(), out List<Handler> handlers)) continue;

            // A failing handler must not cost the other handlers and events.
            foreach (Handler handler in handlers)
            {
                try
                {
                    handler.Call(@event);
                }
                catch (Exception e)
                {
                    _log.Error(e, "[EventHandler] {handler} failed on {event}", handler.Name, @event.GetType().Name);
                }
            }
        }
    }

    // Walks the hierarchy level by level, because GetMethods does not return the private methods of a base class.
    private IEnumerable<MethodInfo> HandlerMethods(Type type)
    {
        for (; type != null; type = type.BaseType)
        {
            foreach (MethodInfo method in type.GetMethods(HandlerFlags))
            {
                if (method.IsDefined(typeof(EventHandlerAttribute)))
                {
                    yield return method;
                }
            }
        }
    }

    // The handler is called through a typed delegate, so no event goes through reflection.
    // The handler's own exception is then not wrapped into a TargetInvocationException.
    private static Action<Event> CreateCall(object owner, MethodInfo method, Type eventType)
    {
        Delegate typed = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(eventType), owner, method);
        return (Action<Event>) WrapMethod.MakeGenericMethod(eventType).Invoke(null, [typed])!;
    }

    private static Action<Event> Wrap<T>(Action<T> handler) where T : Event => @event => handler((T) @event);
}
