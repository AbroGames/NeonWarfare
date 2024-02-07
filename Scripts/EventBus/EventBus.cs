using System;
using System.Collections.Generic;

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
    public void Publish<T>(T eventData)
    {
        if (_subscribers.ContainsKey(typeof(T)))
        {
            var handler = _subscribers[typeof(T)] as Action<T>;
            handler?.Invoke(eventData);
        }
    }
}