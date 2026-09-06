using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Scans running processes for known cheat/injection tools and checks for
/// an attached debugger. Standalone builds only (this obviously does not
/// apply to WebGL/mobile). Not bulletproof on its own -- pair it with
/// server-side validation -- but it catches a large chunk of "script kiddie"
/// tools out of the box.
/// </summary>
public class CheatToolDetector : MonoBehaviour
{
    [Header("Settings")]
    public float checkIntervalSeconds = 10f;

    private static readonly string[] KnownCheatProcessNames = new string[]
    {
        "cheatengine-x86_64",
        "cheatengine-i386",
        "CheatEngine",
        "ArtMoney",
        "GameGuardian",
        "SpeedHack",
        "ExtremeInjector",
        "InjectorGUI",
        "ProcessHacker",
        "x64dbg",
        "ollydbg",
        "ida64",
        "ida"
    };

    public event Action<string> CheatToolDetected;

    float timer;

#if UNITY_STANDALONE_WIN
    [DllImport("kernel32.dll")]
    private static extern bool IsDebuggerPresent();
#endif

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= checkIntervalSeconds)
        {
            timer = 0f;
            CheckProcesses();
            CheckDebugger();
        }
    }

    void CheckProcesses()
    {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        try
        {
            Process[] processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                foreach (var known in KnownCheatProcessNames)
                {
                    if (p.ProcessName.IndexOf(known, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        CheatToolDetected?.Invoke($"Suspicious process detected: {p.ProcessName}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[AntiCheat] Process scan failed: {e.Message}");
        }
#endif
    }

    void CheckDebugger()
    {
#if UNITY_STANDALONE_WIN
        if (IsDebuggerPresent())
        {
            CheatToolDetected?.Invoke("Native debugger attached to process");
        }
#endif
        if (Debugger.IsAttached)
        {
            CheatToolDetected?.Invoke(".NET managed debugger attached");
        }
    }
}
