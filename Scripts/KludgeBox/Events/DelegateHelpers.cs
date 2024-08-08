using System;
using System.Reflection;
using KludgeBox.Networking;

namespace KludgeBox.Events;

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
    public static Action<QueryEvent> FuncToResolvingAction(MethodInfo info)
    {
        return (query) =>
        {
            if (query is IInstanceEvent evt)
            {
                query.SetResult(info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, evt.NetworkId), [query]));
            }
            else
            {
                query.SetResult(info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, null), [query]));
            }
        };
    }

    public static Action<IEvent> InstanceResolvingAction(MethodInfo info)
    {
        return evt =>
        {
            if (evt is IInstanceEvent iEvt)
            {
                info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, iEvt.NetworkId), [iEvt]);
            }
            else
            {
                info.Invoke(NetworkInstance.ResolveInstance(info.DeclaringType, null), [evt]);
            }
        };
    }
}