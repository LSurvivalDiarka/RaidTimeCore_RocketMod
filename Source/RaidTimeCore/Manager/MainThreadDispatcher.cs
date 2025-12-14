using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace RaidTimeCore.Manager;

internal static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> _queue = new();
    private static DispatcherBehaviour? _behaviour;

    public static void Initialize()
    {
        if (_behaviour != null) return;

        var go = new GameObject("RaidTimeCore.MainThreadDispatcher");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _behaviour = go.AddComponent<DispatcherBehaviour>();
    }

    public static void Shutdown()
    {
        var b = _behaviour;
        _behaviour = null;

        if (b == null) return;
        try { UnityEngine.Object.Destroy(b.gameObject); }
        catch { }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        _queue.Enqueue(action);
    }

    private sealed class DispatcherBehaviour : MonoBehaviour
    {
        private void Update()
        {
            while (_queue.TryDequeue(out var a))
            {
                try { a(); }
                catch { }
            }
        }
    }
}
