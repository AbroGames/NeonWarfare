using System;
using System.Reflection;
using KludgeBox.Events;

namespace NeoVector.KludgeBox.Events;

public readonly record struct ListenerInfo<T>(Action<T> Action, bool IsDefault, MethodInfo Source) where T : IEvent;