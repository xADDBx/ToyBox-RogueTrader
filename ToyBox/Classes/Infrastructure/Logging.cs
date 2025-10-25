using System.Diagnostics;

namespace ToyBox.Infrastructure;

public static class Logging {
    [StackTraceHidden]
    internal static void LogEarly(string str) {
        Main.ModEntry.Logger.Log(str);
    }
    [StackTraceHidden]
    public static void Trace(string str) {
        if (Settings.LogLevel >= LogLevel.Trace) {
            Main.ModEntry.Logger.Log($"[Trace] {str}");
        }
    }
    [StackTraceHidden]
    public static void Debug(string str) {
        if (Settings.LogLevel >= LogLevel.Debug) {
            Main.ModEntry.Logger.Log($"[Debug] {str}");
        }
    }
    [StackTraceHidden]
    public static void Log(string str) {
        if (Settings.LogLevel >= LogLevel.Info) {
            Main.ModEntry.Logger.Log(str);
        }
    }
    [StackTraceHidden]
    public static void Warn(string str) {
        if (Settings.LogLevel >= LogLevel.Warning) {
            Main.ModEntry.Logger.Warning(str);
        }
    }
    [StackTraceHidden]
    public static void Critical(Exception ex) {
        Main.ModEntry.Logger.Critical(ex.ToString());
    }
    [StackTraceHidden]
    public static void Critical(string str, bool includeStackTrace = true) {
        Main.ModEntry.Logger.Error($"{str}:\n{(includeStackTrace ? new StackTrace(true).ToString() : "")}");
    }
    [StackTraceHidden]
    public static void Error(Exception ex) {
        Main.ModEntry.Logger.Error(ex.ToString());
    }
    [StackTraceHidden]
    public static void Error(string str, bool includeStackTrace = true) {
        Main.ModEntry.Logger.Error($"{str}:\n{(includeStackTrace ? new StackTrace(true).ToString() : "")}");
    }
    [StackTraceHidden]
    public static void LogHistory(string str) {
        Main.ModEntry.Logger.Log($"[History] {str}");
    }
}
