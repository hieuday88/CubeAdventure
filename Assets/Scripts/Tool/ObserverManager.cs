using System;
using System.Collections.Generic;
using UnityEngine;

public static class ObserverManager<T> where T : Enum
{
    private static readonly Dictionary<T, Action<object>> _boardObserver = new Dictionary<T, Action<object>>();

    static ObserverManager()
    {
        ObserverManagerRegistry.Register(Clear);
    }

    public static void AddListener(T eventId, Action<object> callback)
    {
        if (callback == null)
        {
            return;
        }

        if (_boardObserver.TryGetValue(eventId, out Action<object> existing))
        {
            _boardObserver[eventId] = existing + callback;
            return;
        }

        _boardObserver.Add(eventId, callback);
    }

    public static void Post(T eventId, object payload = null)
    {
        if (!_boardObserver.TryGetValue(eventId, out Action<object> callback))
        {
            return;
        }

        if (callback == null)
        {
            _boardObserver.Remove(eventId);
            return;
        }

        callback.Invoke(payload);
    }

    public static void RemoveListener(T eventId, Action<object> callback)
    {
        if (callback == null)
        {
            return;
        }

        if (!_boardObserver.TryGetValue(eventId, out Action<object> existing))
        {
            return;
        }

        existing -= callback;

        if (existing == null)
        {
            _boardObserver.Remove(eventId);
            return;
        }

        _boardObserver[eventId] = existing;
    }

    public static void RemoveAllListeners(T eventId)
    {
        _boardObserver.Remove(eventId);
    }

    public static void Clear()
    {
        _boardObserver.Clear();
    }

    [Obsolete("Use AddListener(eventId, callback)")]
    public static void AddDesgisterEvent(T evenID, Action<object> callback) => AddListener(evenID, callback);

    [Obsolete("Use Post(eventId, payload)")]
    public static void PostEven(T evenID, object parant = null) => Post(evenID, parant);

    [Obsolete("Use RemoveListener(eventId, callback)")]
    public static void RemoveAddListener(T evenID, Action<object> callback) => RemoveListener(evenID, callback);

    [Obsolete("Use Clear()")]
    public static void RemoveAll() => Clear();
}

internal static class ObserverManagerRegistry
{
    private static readonly HashSet<Action> _resetters = new HashSet<Action>();

    public static void Register(Action resetter)
    {
        if (resetter == null)
        {
            return;
        }

        _resetters.Add(resetter);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        foreach (Action resetter in _resetters)
        {
            resetter.Invoke();
        }
    }
}
