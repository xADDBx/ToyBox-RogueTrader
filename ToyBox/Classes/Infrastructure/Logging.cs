namespace ToyBox.Infrastructure;

public static class Logging {
    internal static void LogEarly(string str) {
        Main.ModEntry.Logger.Log(str);
    }
    public static void Trace(string str) {
        if (Settings.LogLevel >= LogLevel.Trace) {
            Main.ModEntry.Logger.Log($"[Trace] {str}");
        }
    }
    public static void Debug(string str) {
        if (Settings.LogLevel >= LogLevel.Debug) {
            Main.ModEntry.Logger.Log($"[Debug] {str}");
        }
    }
    public static void Log(string str) {
        if (Settings.LogLevel >= LogLevel.Info) {
            Main.ModEntry.Logger.Log(str);
        }
    }
    public static void Warn(string str) {
        if (Settings.LogLevel >= LogLevel.Warning) {
            Main.ModEntry.Logger.Warning(str);
        }
    }
    public static void Critical(Exception ex) {
        Main.ModEntry.Logger.Critical(ex.ToString());
    }
    public static void Critical(string str, bool includeStackTrace = true) {
        Main.ModEntry.Logger.Error($"{str}:\n{(includeStackTrace ? new System.Diagnostics.StackTrace(1, true).ToString() : "")}");
    }
    public static void Error(Exception ex) {
        Main.ModEntry.Logger.Error(ex.ToString());
    }
    public static void Error(string str, int skip = 1, bool includeStackTrace = true) {
        Main.ModEntry.Logger.Error($"{str}:\n{(includeStackTrace ? new System.Diagnostics.StackTrace(skip, true).ToString() : "")}");
    }
    public static void LogHistory(string str) {
        Main.ModEntry.Logger.Log($"[History] {str}");
    }
}
