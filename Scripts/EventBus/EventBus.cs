using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KludgeBox;
using KludgeBox.Events;

public class EventBus
{
    // Словарь для хранения событий и их подписчиков
    private readonly Dictionary<Type, Delegate> _subscribers = new();

    // Метод для подписки на событие
    public void Subscribe<T>(Action<T> handler)
    {
        if (_subscribers.ContainsKey(typeof(T)))
        {
            _subscribers[typeof(T)] = Delegate.Combine(_subscribers[typeof(T)], handler);
        }
        else
        {
            _subscribers.Add(typeof(T), handler);
        }
    }

    public void SubscribeMethod(MethodInfo methodInfo, object invoker)
    {
        Type messageType = methodInfo.GetParameters()[0].ParameterType;

        // Create an Action<TArg> delegate from the MethodInfo
        var delegateType = typeof(Action<>).MakeGenericType(messageType);
        var actionDelegate = Delegate.CreateDelegate(delegateType, null, methodInfo);

        // Subscribe to the message type using the created delegate
        typeof(EventBus).GetMethod("Subscribe")!.MakeGenericMethod(messageType)
            .Invoke(this, new object[] { actionDelegate });
    }

    // Метод для отписки от события
    public void Unsubscribe<T>(Action<T> handler)
    {
        if (_subscribers.ContainsKey(typeof(T)))
        {
            var currentDel = _subscribers[typeof(T)];
            _subscribers[typeof(T)] = Delegate.Remove(currentDel, handler);

            if (_subscribers[typeof(T)] == null)
            {
                _subscribers.Remove(typeof(T));
            }
        }
    }

    // Метод для публикации события
    // TODO: добавить класс Publisher с прямой ссылкой на делегат, чтоб каждый пердёж не начинал поиск в хэшмапе
    // Publisher целесообразно запихать во все Process и PhysicsProcess
    public void Publish<T>(T eventData)
    {
        if (_subscribers.TryGetValue(typeof(T), out var handler))
        {
            (handler as Action<T>)?.Invoke(eventData);
        }
    }
    
    public void RegisterListeners(ServiceRegistry registry)
    {
        foreach (var service in registry.Services)
        {
            var type = service.GetType();
            var methods = type.GetMethods();
            var voidReturns = methods.Where(method => method.ReturnType == typeof(void));
            var singleParameter = voidReturns.Where(x => x.GetParameters().Length == 1);
            var listeners = singleParameter
                .Where(x => x.GetCustomAttributes(typeof(GameEventListenerAttribute), false)
                    .FirstOrDefault() != null);

            foreach (var listener in listeners)
            {
                SubscribeMethod(listener, service);
            }
        }
    }
}


public class GameEventListenerAttribute : Attribute;