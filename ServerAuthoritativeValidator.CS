using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// The most important anti-cheat layer for any multiplayer game: NEVER trust
/// the client. This is a generic helper that goes on your SERVER-side object
/// and rate-limits / range-validates actions before you apply them, no
/// matter which networking stack you use (Mirror [Command], Netcode
/// ServerRpc, Fishnet [ServerRpc], Photon RPC, etc). Call ValidateAction /
/// ValidateRange / ValidatePositionDelta at the top of each RPC handler.
/// </summary>
public class ServerAuthoritativeValidator : MonoBehaviour
{
    [Serializable]
    public class ActionLimit
    {
        public string actionName;
        [Tooltip("Minimum seconds allowed between two calls of this action.")]
        public float minInterval = 0.1f;
        [Tooltip("Max calls allowed inside the rolling window below.")]
        public int maxPerWindow = 20;
        public float windowSeconds = 1f;
    }

    [Tooltip("Define a rule per action name, e.g. 'Fire', 'UseItem', 'Jump'.")]
    public List<ActionLimit> actionLimits = new List<ActionLimit>();

    private Dictionary<(int playerId, string action), float> lastActionTime = new();
    private Dictionary<(int playerId, string action), Queue<float>> actionWindow = new();

    public event Action<int, string> SuspiciousActivity;

    /// <summary>Call this first, inside the server RPC, before doing anything else.</summary>
    public bool ValidateAction(int playerId, string actionName)
    {
        ActionLimit limit = actionLimits.Find(a => a.actionName == actionName);
        if (limit == null) return true; // no rule defined = allowed by default

        float now = Time.time;
        var key = (playerId, actionName);

        if (lastActionTime.TryGetValue(key, out float last) && now - last < limit.minInterval)
        {
            SuspiciousActivity?.Invoke(playerId, $"Action '{actionName}' spam (interval {now - last:F3}s < {limit.minInterval}s)");
            return false;
        }
        lastActionTime[key] = now;

        if (!actionWindow.TryGetValue(key, out var q))
        {
            q = new Queue<float>();
            actionWindow[key] = q;
        }
        q.Enqueue(now);
        while (q.Count > 0 && now - q.Peek() > limit.windowSeconds)
            q.Dequeue();

        if (q.Count > limit.maxPerWindow)
        {
            SuspiciousActivity?.Invoke(playerId, $"Action '{actionName}' exceeded {limit.maxPerWindow} calls in {limit.windowSeconds}s");
            return false;
        }

        return true;
    }

    /// <summary>Use for damage amounts, currency deltas, stat values sent by a client.</summary>
    public bool ValidateRange(float value, float min, float max, int playerId, string context)
    {
        if (value < min || value > max)
        {
            SuspiciousActivity?.Invoke(playerId, $"'{context}' out of range: {value} (expected {min}-{max})");
            return false;
        }
        return true;
    }

    /// <summary>Use to reject impossible teleports/position updates sent by a client.</summary>
    public bool ValidatePositionDelta(Vector3 oldPos, Vector3 newPos, float maxDeltaPerCall, int playerId)
    {
        float dist = Vector3.Distance(oldPos, newPos);
        if (dist > maxDeltaPerCall)
        {
            SuspiciousActivity?.Invoke(playerId, $"Position delta too large: {dist:F2} (max {maxDeltaPerCall})");
            return false;
        }
        return true;
    }
}
