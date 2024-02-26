using System;
using System.Reflection;

namespace KludgeBox.Events;

public static class QueryHelpers
{
    public static bool IsQueryEvent(Type type)
    {
        return type.IsAssignableTo(typeof(QueryEvent));
    }

    public static bool IsQueryListener(MethodInfo info)
    {
        return info.ReturnType != typeof(void);
    }

    public static Action<QueryEvent> ToAction(object invoker, MethodInfo info)
    {
        return (query) => { query.SetResult(info.Invoke(invoker, [query])); };
    }

}