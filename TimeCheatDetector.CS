using UnityEngine;
using System;

/// <summary>
/// Catches two common cheats: (1) unauthorized Time.timeScale changes
/// (used to speed up cooldowns/animations) and (2) system clock manipulation
/// (used to bypass real-time timers like energy regen or daily rewards),
/// by comparing engine realtime against the OS clock.
/// </summary>
public class TimeCheatDetector : MonoBehaviour
{
    [Header("Settings")]
    public float checkInterval = 5f;
    public float clockDriftToleranceSeconds = 2f;
    public bool allowTimeScaleChanges = false;

    private float timer;
    private DateTime lastRealWorldTime;
    private float lastRealtimeSinceStartup;

    public event Action<string> CheatDetected;

    void Start()
    {
        lastRealWorldTime = DateTime.UtcNow;
        lastRealtimeSinceStartup = Time.realtimeSinceStartup;
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (!allowTimeScaleChanges && Mathf.Abs(Time.timeScale - 1f) > 0.01f)
        {
            CheatDetected?.Invoke($"Unauthorized Time.timeScale change detected: {Time.timeScale}");
        }

        if (timer >= checkInterval)
        {
            CheckClockDrift();
            timer = 0f;
        }
    }

    void CheckClockDrift()
    {
        DateTime now = DateTime.UtcNow;
        float engineElapsed = Time.realtimeSinceStartup - lastRealtimeSinceStartup;
        float systemElapsed = (float)(now - lastRealWorldTime).TotalSeconds;

        float drift = Mathf.Abs(systemElapsed - engineElapsed);
        if (drift > clockDriftToleranceSeconds)
        {
            CheatDetected?.Invoke($"System clock drift detected: {drift:F2}s (possible clock manipulation)");
        }

        lastRealWorldTime = now;
        lastRealtimeSinceStartup = Time.realtimeSinceStartup;
    }
}
