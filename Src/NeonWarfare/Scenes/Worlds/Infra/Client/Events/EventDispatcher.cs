using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Humanizer;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Client.Events;

/// <summary>
/// Delivers a received events section to the <see cref="EventHandlerAttribute"/> methods of the Presentation.
/// The handlers are collected once, by the composition root, since MS.DI cannot inject "every Presentation".
/// </summary>
[Client]
public class EventDispatcher(NetMessageCodec codec)
{
    private const string NoHandlerLog = "{event} has no [EventHandler]: it will be read and dropped";
    private const string HandlerFailedLog = "[EventHandler] {handler} failed on {event}";
    private const string RegisteredError = "The event handlers are already registered.";
    private const string NotRegisteredError = "The event handlers are not registered yet.";
    private const string ParameterCountError = "[EventHandler] {0} must have exactly one parameter.";
    private const string NotEventError = "[EventHandler] {0} takes {1}, which is not an event type.";
    private const string EmptyPacketError = "The packet is empty.";
    private const string NotEventsPacketError = "The packet kind is {0}, not an events packet.";
    private const string TrailingBytesError = "{0} of {1} bytes read, an events packet carries one section.";
    
    private record Handler(string Name, Action<Event> Call);

    private const BindingFlags HandlerFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly MethodInfo WrapMethod =
        typeof(EventDispatcher).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly ILogger _log = LogFactory.GetForStatic<EventDispatcher>();

    private readonly Dictionary<Type, List<Handler>> _handlersByType = new();
    private IReadOnlySet<Type> _eventTypes;

    public void Register(IEnumerable<object> owners, IReadOnlySet<Type> eventTypes)
    {
        if (_eventTypes != null)
        {
            throw new InvalidOperationException(RegisteredError);
        }

        foreach (object owner in owners)
        {
            foreach (MethodInfo method in HandlerMethods(owner.GetType()))
            {
                string name = $"{method.DeclaringType!.Name}.{method.Name}";
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    throw new InvalidOperationException(ParameterCountError.FormatWith(name));
                }

                Type eventType = parameters[0].ParameterType;
                if (!eventTypes.Contains(eventType))
                {
                    throw new InvalidOperationException(NotEventError.FormatWith(name, eventType.FullName));
                }

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
            _log.Warning(NoHandlerLog, eventType.Name);
        }
        _eventTypes = eventTypes;
    }

    /// <summary>
    /// The client entry point for an events packet, both from the network and from the host's own loopback.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The packet is not an events packet or is broken.</exception>
    public void DispatchPacket(ReadOnlyMemory<byte> packet)
    {
        if (packet.IsEmpty)
        {
            throw new NetMessageFormatException(EmptyPacketError);
        }
        if (packet.Span[0] != (byte) ServerPacketKind.Events)
        {
            throw new NetMessageFormatException(NotEventsPacketError.FormatWith(packet.Span[0]));
        }

        ReadOnlyMemory<byte> section = packet[1..];
        IReadOnlyList<object> events = Read(section, out int bytesRead);
        if (bytesRead != section.Length)
        {
            throw new NetMessageFormatException(TrailingBytesError.FormatWith(bytesRead, section.Length));
        }
        Deliver(events);
    }

    /// <summary>
    /// The whole section is read before any handler runs, so a broken one changes nothing.
    /// </summary>
    /// <returns>The number of bytes the section took.</returns>
    /// <exception cref="NetMessageFormatException">The section is broken.</exception>
    public int Dispatch(ReadOnlyMemory<byte> section)
    {
        Deliver(Read(section, out int bytesRead));
        return bytesRead;
    }

    private IReadOnlyList<object> Read(ReadOnlyMemory<byte> section, out int bytesRead)
    {
        if (_eventTypes == null)
        {
            throw new InvalidOperationException(NotRegisteredError);
        }

        return codec.ReadSection(section, _eventTypes, out bytesRead);
    }

    private void Deliver(IReadOnlyList<object> events)
    {
        foreach (Event @event in events.Cast<Event>())
        {
            if (!_handlersByType.TryGetValue(@event.GetType(), out List<Handler> handlers)) continue;

            // One failing handler must not cost the rest of the batch
            foreach (Handler handler in handlers)
            {
                try
                {
                    handler.Call(@event);
                }
                catch (Exception e)
                {
                    _log.Error(e, HandlerFailedLog, handler.Name, @event.GetType().Name);
                }
            }
        }
    }

    // Level by level: GetMethods does not return the private methods of a base class
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

    // A typed delegate rather than MethodInfo.Invoke: no reflection per event, and the handler's own exception
    // is not wrapped into a TargetInvocationException
    private static Action<Event> CreateCall(object owner, MethodInfo method, Type eventType)
    {
        Delegate typed = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(eventType), owner, method);
        return (Action<Event>) WrapMethod.MakeGenericMethod(eventType).Invoke(null, [typed])!;
    }

    private static Action<Event> Wrap<T>(Action<T> handler) where T : Event => @event => handler((T) @event);
}
