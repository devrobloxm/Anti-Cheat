using UnityEngine;
using System;

/// <summary>
/// Drop-in replacement for a plain "int" field that protects it against
/// memory scanners/editors (e.g. Cheat Engine) by storing the value XOR'd
/// with a rotating random key plus a checksum. A raw memory search for
/// "health = 100" will never find a matching value, and any external write
/// to the raw bytes breaks the checksum and gets reported.
///
/// Usage: replace  public int health = 100;
///        with     public SecureInt health = 100;
/// It behaves like a normal int via implicit conversion.
/// </summary>
[Serializable]
public struct SecureInt
{
    private int obfuscatedValue;
    private int key;
    private int checksum;

    public SecureInt(int value)
    {
        key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        obfuscatedValue = value ^ key;
        checksum = ComputeChecksum(value, key);
    }

    static int ComputeChecksum(int value, int key)
    {
        unchecked
        {
            return (value * 397) ^ key ^ 0x5F3759DF;
        }
    }

    public int Value
    {
        get
        {
            int real = obfuscatedValue ^ key;
            if (ComputeChecksum(real, key) != checksum)
            {
                Debug.LogWarning("[AntiCheat] Memory tampering detected on SecureInt read!");
                MemoryIntegrityChecker.ReportTamper("SecureInt checksum mismatch");
            }
            return real;
        }
        set
        {
            key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            obfuscatedValue = value ^ key;
            checksum = ComputeChecksum(value, key);
        }
    }

    public static implicit operator int(SecureInt s) => s.Value;
    public static implicit operator SecureInt(int v) => new SecureInt(v);
}

/// <summary>
/// Central hub that any SecureInt (or other secure wrapper you add later)
/// reports tampering to. Subscribe once from your AntiCheatManager.
/// </summary>
public static class MemoryIntegrityChecker
{
    public static event Action<string> TamperDetected;

    public static void ReportTamper(string reason)
    {
        TamperDetected?.Invoke(reason);
    }
}
