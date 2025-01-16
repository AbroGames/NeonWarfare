using System;
using System.Reflection;

namespace NeonWarfare.Scripts.KludgeBox.Events;

public readonly record struct ListenerInfo<T>(Action<T> Action, bool IsDefault, MethodInfo Source, bool MustBeResolved) where T : IEvent;
