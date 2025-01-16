using System;
using System.Reflection;
using NeonWarfare.Scripts.KludgeBox.Events.EventTypes;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scripts.KludgeBox.Events;

public static class DelegateHelpers
{
    public static bool IsQueryEvent(Type type)
    {
        return type.IsAssignableTo(typeof(QueryEvent));
    }

    public static bool IsQueryListener(MethodInfo info)
    {
        return info.ReturnType != typeof(void);
    }

    public static Action<QueryEvent> FuncToAction(object invoker, MethodInfo info)
    {
        return (query) => { query.SetResult(info.Invoke(invoker, [query])); };
    }
    
    private static Network NetworkInstance => Network.Instance;
    public static Action<QueryEvent> FuncToResolvingAction(MethodInfo info, Type eventType)
    {
        var queryResolver = (QueryEvent query) =>
        {
            if (query is IInstanceEvent evt)
            {
                query.SetResult(info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, evt.NetworkId), [query]));
            }
            else
            {
                query.SetResult(info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, query), [query]));
            }
        };

        return queryResolver;
    }

    public static Delegate InstanceResolvingAction(MethodInfo info, Type eventType)
    {
        var resolver = (IEvent evt) =>
        {
            if (evt is IInstanceEvent iEvt)
            {
                info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, iEvt.NetworkId), [iEvt]);
            }
            else
            {
                info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, evt), [evt]);
            }
        };

        return DoCastingMagic(resolver, eventType);
    }
    
    private static Delegate DoCastingMagic(Action<IEvent> action, Type eventType)
    {
        if(!eventType.IsAssignableTo(typeof(IEvent)))
            throw new ArgumentException("Provided type does not implement IEvent");

        var generalizer = typeof(DelegateHelpers)
            .GetMethod(nameof(GeneralizedAction), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod([eventType]);
            
        var method = generalizer.Invoke(null, [action]) as Delegate;
        
        return method;
    }
    
    private static Action<T> GeneralizedAction<T>(Action<IEvent> action) where T : IEvent
    {
        var newAction = (T evt) =>
        {
            action?.Invoke(evt);
        };

        return newAction;
    }
}
