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
    public static IEnumerable<MethodSubscriptionInfo> ScanEventListeners()
    {
        return ScanStaticEventListenersOfType(typeof(IEvent));
    }

    /// <summary>
    /// Scans for event listener methods of the specified <paramref name="paramType"/>.
    /// </summary>
    /// <param name="paramType">The type of events for which listeners should be scanned.</param>
    /// <returns>An enumerable collection of <see cref="MethodInfo"/> representing event listener methods.</returns>
    public static IEnumerable<MethodSubscriptionInfo> ScanStaticEventListenersOfType(Type paramType)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies(); // Returns all currently loaded assemblies
        var types = assemblies.SelectMany(x => x.GetTypes()); // returns all types defined in these assemblies
        var classes = types.Where(x => x.IsClass); // only yields classes
        var methods = classes.SelectMany(x => x.GetMethods()); // returns all methods defined in those classes
        var staticMethods = methods.Where(x => x.IsStatic); // returns all methods defined in those classes
        var voidReturns = staticMethods.Where(method => method.ReturnType == typeof(void)); // method should return void
        var singleParameter = voidReturns.Where(x => x.GetParameters().Length == 1); // method should accept only one parameter
        var rightParamType = singleParameter.Where(x => x.GetParameters().First().ParameterType.IsAssignableTo(paramType)); // and that parameter must be assignable to a variable of type
        var listeners = rightParamType.Where(x => x.GetCustomAttributes(typeof(EventListenerAttribute), false).FirstOrDefault() != null); // returns only methods that have the EventListener attribute

        var subscriptionInfo = listeners.Select(method => 
            new MethodSubscriptionInfo(method, null, method.GetCustomAttribute<EventListenerAttribute>()!.Priority));
        
        return subscriptionInfo;
    }

    public static IEnumerable<MethodSubscriptionInfo> ScanEventListenersInTypesOfType(object[] listenerSources,
        Type paramType)
    {
        List<MethodSubscriptionInfo> subscriptions = new();
        foreach (var source in listenerSources)
        {
            var type = source.GetType();
            var rawMethods = type.GetMethods();
            var withAttribute = rawMethods.Where(x => x.GetCustomAttributes(typeof(EventListenerAttribute), false).FirstOrDefault() != null);
            var singleParameter = withAttribute.Where(x => x.GetParameters().Length == 1);
            var methods = singleParameter.Where(x => x.GetParameters().First().ParameterType.IsAssignableTo(paramType));
            
            foreach (MethodInfo method in methods)
            {
                object invoker = method.IsStatic ? null : source;
                ListenerPriority priority = method.GetCustomAttribute<EventListenerAttribute>()!.Priority;

                subscriptions.Add(new MethodSubscriptionInfo(method, invoker, priority));
            }
        }

        return subscriptions.AsReadOnly();
    }

    /// <summary>
    /// Subscribes a collection of event listener methods to the specified <paramref name="targetEventBus"/>.
    /// </summary>
    /// <param name="targetEventBus">The event bus to which the methods should be subscribed.</param>
    /// <param name="methods">An enumerable collection of <see cref="MethodInfo"/> representing event listener methods.</param>
    public static void SubscribeMethods(this EventBus targetEventBus, IEnumerable<MethodSubscriptionInfo> methods)
    {
        foreach (var method in methods)
        {
            targetEventBus.SubscribeMethod(method);
        }
    }
}

public record MethodSubscriptionInfo(MethodInfo Method, object Invoker, ListenerPriority Priority);