using System;

using UnityEngine;

public static class GCDebug
{
    private const string TAG = "[GC]"; //가을 채원

    private const string COLOR_INFO = "#FFFFFF"; // 기본 정보 (하얀색)
    private const string COLOR_SUCCESS = "#81C784"; // 성공 (초록)
    private const string COLOR_WARNING = "#FFD54F"; // 경고 (노랑)
    private const string COLOR_ERROR = "#E57373"; // 에러 (빨강)

    public static Action<string> OnLog;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        Debug.Log(Format(COLOR_INFO, message));
        OnLog?.Invoke(Format(COLOR_INFO, message));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Success(string message)
    {
        Debug.Log(Format(COLOR_SUCCESS, message));
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Fail(string message)
    {
        Debug.Log(Format(COLOR_ERROR, message));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Warning(string message)
    {
        Debug.LogWarning(Format(COLOR_WARNING, message));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Error(string message)
    {
        Debug.LogError(Format(COLOR_ERROR, message));
        OnLog?.Invoke(Format(COLOR_ERROR, message));
    }

    private static string Format(string color, string message)
    {
        return $"<color={color}>{TAG}</color> {message}";
    }
}
