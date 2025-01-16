using System;
using System.Reflection;
using NeonWarfare.Scripts.KludgeBox.Events.EventTypes;

namespace NeonWarfare.Scripts.KludgeBox.Events;

public readonly record struct ListenerInfo<T>(Action<T> Action, bool IsDefault, MethodInfo Source, bool MustBeResolved) where T : IEvent;
