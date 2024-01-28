using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace KludgeBox.Events;


/// <summary>
/// Provides functionality to scan for event listener methods and subscribe them to an event bus.
/// </summary>
public static class EventScanner
{
    /// <summary>
    /// Scans for event listener methods of type <see cref="IEvent"/>.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="MethodInfo"/> representing event listener methods.</returns>
    public static IEnumerable<MethodInfo> ScanEventListeners()
    {
        return ScanEventListenersOfType(typeof(IEvent));
    }

    /// <summary>
    /// Scans for event listener methods of the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type of events for which listeners should be scanned.</param>
    /// <returns>An enumerable collection of <see cref="MethodInfo"/> representing event listener methods.</returns>
    public static IEnumerable<MethodInfo> ScanEventListenersOfType(Type type)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies(); // Returns all currently loaded assemblies
        var types = assemblies.SelectMany(x => x.GetTypes()); // returns all types defined in these assemblies
        var classes = types.Where(x => x.IsClass); // only yields classes
        var methods = classes.SelectMany(x => x.GetMethods()); // returns all methods defined in those classes
        var staticMethods = methods.Where(x => x.IsStatic); // returns all methods defined in those classes
        var voidReturns = staticMethods.Where(method => method.ReturnType == typeof(void)); // method should return void
        var singleParameter = voidReturns.Where(x => x.GetParameters().Length == 1); // method should accept only one parameter
        var rightParamType = singleParameter.Where(x => x.GetParameters().First().ParameterType.IsAssignableTo(type)); // and that parameter must be assignable to a variable of type
        var listeners = rightParamType.Where(x => x.GetCustomAttributes(typeof(EventListenerAttribute), false).FirstOrDefault() != null); // returns only methods that have the EventListener attribute
        
        return listeners;
    }

    /// <summary>
    /// Subscribes a collection of event listener methods to the specified <paramref name="targetEventBus"/>.
    /// </summary>
    /// <param name="targetEventBus">The event bus to which the methods should be subscribed.</param>
    /// <param name="methods">An enumerable collection of <see cref="MethodInfo"/> representing event listener methods.</param>
    public static void SubscribeMethods(this EventBus targetEventBus, IEnumerable<MethodInfo> methods)
    {
        foreach (var method in methods)
        {
            var priority = method.GetCustomAttribute<EventListenerAttribute>()!.Priority;
            targetEventBus.SubscribeMethod(method, priority);
        }
    }
}