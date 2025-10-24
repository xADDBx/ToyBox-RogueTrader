namespace ToyBox.Infrastructure.Enums;
public enum LogLevel {
    Error,
    Warning,
    Info,
    Debug,
    Trace
}
public static partial class LogLevel_Localizer {
    public static string GetLocalized(this LogLevel type) {
        return type switch {
            LogLevel.Error => m_ErrorText,
            LogLevel.Warning => m_WarningText,
            LogLevel.Info => m_InfoText,
            LogLevel.Debug => m_DebugText,
            LogLevel.Trace => m_TraceText,
            _ => "!!Error Unknown LogLevel!!",
        };
    }

    [LocalizedString("ToyBox_Infrastructure_Enums_LogLevel_Localizer_ErrorText", "Error")]
    private static partial string m_ErrorText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_LogLevel_Localizer_WarningText", "Warning")]
    private static partial string m_WarningText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_LogLevel_Localizer_InfoText", "Info")]
    private static partial string m_InfoText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_LogLevel_Localizer_DebugText", "Debug")]
    private static partial string m_DebugText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_LogLevel_Localizer_TraceText", "Trace")]
    private static partial string m_TraceText { get; }
}
