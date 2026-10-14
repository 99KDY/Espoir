using System;
using System.Collections.Generic;
using UnityEngine;

public static class CLogger
{
    public static bool Enable = true;
    public static bool EnableRichText = true;

    private static int _indentLevel = 0;
    private const int INDENT_SPACES = 2;

    private static readonly HashSet<string> _onceSet = new HashSet<string>();
    private static string Indent
    {
        get
        {
            return new string(' ', _indentLevel * INDENT_SPACES);
        }
    }

    public static void IndentPush() => _indentLevel++;
    public static void IndentPop()
    {
        _indentLevel--;
        if (_indentLevel < 0) _indentLevel = 0;
    }
    public static void IndentReset() => _indentLevel = 0;
    private enum ELogKind
    {
        Log,
        Warn,
        Error,
        Success
    }

    private static void Emit(ELogKind kind, string msg, string tag = null, string colorHex = null)
    {
        if (!Enable) return;

        string prefix = string.Empty;
        if (!string.IsNullOrEmpty(tag))
        {
            if (EnableRichText && !string.IsNullOrEmpty(colorHex))
            {
                prefix = $"<color={colorHex}>[{tag}] </color>";
            }
            else
            {
                prefix = $"[{tag}] ";
            }
        }

        string final = $"{Indent}{prefix}{msg}";
        switch (kind)
        {
            case ELogKind.Log:
                Debug.Log(final);
                break;
            case ELogKind.Warn:
                Debug.LogWarning(final);
                break;
            case ELogKind.Error:
                Debug.LogError(final);
                break;
            case ELogKind.Success:
                Debug.Log(final);
                break;
        }
    }

    public static void Log(string msg)
    {
        Emit(ELogKind.Log, msg);
    }
    public static void Warn(string msg)
    {
        Emit(ELogKind.Warn, msg, "WARN", "#FF9100");
    }
    public static void Error(string msg)
    {
        Emit(ELogKind.Error, msg, "ERROR", "#FF1744");
    }
    public static void Success(string msg)
    {
        Emit(ELogKind.Success, msg, "OK", "#00C853");
    }

    public static void V3(string label, Vector3 v, int digits = 2)
    {
        float x = (float)System.Math.Round(v.x, digits);
        float y = (float)System.Math.Round(v.y, digits);
        float z = (float)System.Math.Round(v.z, digits);

        Emit(ELogKind.Log, $"{label} : ({x}, {y}, {z})");
    }

    public static void KV(string key, object value)
    {
        // KV = key = value 형태로 값을 찍는 표준 포맷 헬퍼
        // 가장 자주 찍는 형태 -> 포맷 통일
        Log($"{key} = {value}");
    }

    public static void Group(string title, Action body, char lineCh = '=', int LineCount = 20)
    {
        if (!Enable)
        {
            return;
        }

        Log($"===== {title} =====");
        IndentPush();
        // 실행할 코드 블록을 호출(Action)
        // ?.Invoke() : body가 null이면 실행하지 않겠다. (예외 방지)
        body?.Invoke();
        IndentPop();
        Log($"===============");
    }
    public static void Once(string key, string msg)
    {
        if (!Enable) return;
        // 이미 키가 있는 경우 -> 재출력 금지
        if (_onceSet.Contains(key)) return;
        _onceSet.Add(key);
        Warn($"[ONCE] {msg}");
    }
    public static void OnceClear()
    {
        _onceSet.Clear();
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Ray(Vector3 origin, Vector3 direction, Color color, float duration = 0f)
    {
        if (!Enable) return;
        Debug.DrawLine(origin, origin + direction, color, duration);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Line3D(Vector3 a, Vector3 b, Color color, float duration = 0f)
    {
        if (!Enable)
        {
            return;
        }
        Debug.DrawLine(a, b, color, duration);
    }
}