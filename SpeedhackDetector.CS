using UnityEngine;

/// <summary>
/// Detects abnormal movement speed by comparing actual distance traveled
/// against Time.unscaledDeltaTime (immune to Time.timeScale tricks).
/// Attach to the player's root object. Works client-side as a first filter,
/// but the AUTHORITATIVE check should always happen on the server too.
/// </summary>
public class SpeedHackDetector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Max legit speed in units/sec (e.g. character controller max speed).")]
    public float maxAllowedSpeed = 10f;
    [Tooltip("Extra margin for lag/physics jitter.")]
    public float toleranceMultiplier = 1.25f;
    [Tooltip("How many consecutive violations before we raise the flag.")]
    public int violationsBeforeFlag = 3;
    [Tooltip("How often (sec) to sample position.")]
    public float checkInterval = 0.5f;

    private Vector3 lastPosition;
    private float timer;
    private int violationCount;

    public delegate void OnCheatDetected(string reason);
    public event OnCheatDetected CheatDetected;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // unscaledDeltaTime so Time.timeScale abuse doesn't skew the math
        timer += Time.unscaledDeltaTime;
        if (timer >= checkInterval)
        {
            CheckSpeed(timer);
            timer = 0f;
        }
    }

    void CheckSpeed(float elapsed)
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        float speed = distance / elapsed;
        float allowedMax = maxAllowedSpeed * toleranceMultiplier;

        if (speed > allowedMax)
        {
            violationCount++;
            Debug.LogWarning($"[AntiCheat] Speed violation #{violationCount}: {speed:F2} u/s (max {allowedMax:F2})");

            if (violationCount >= violationsBeforeFlag)
            {
                CheatDetected?.Invoke($"Sustained speed hacking detected: {speed:F2} u/s (limit {allowedMax:F2})");
                violationCount = 0;
            }
        }
        else
        {
            violationCount = Mathf.Max(0, violationCount - 1);
        }

        lastPosition = transform.position;
    }
}
