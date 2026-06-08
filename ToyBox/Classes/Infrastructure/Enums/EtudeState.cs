namespace ToyBox.Infrastructure.Enums;

public enum EtudeState {
    NotStarted = 0,
    Started = 1,
    Active = 2,
    PreCompleted = 3,
    Completed = 4,
    CompletionInProgress = 5
}
public static partial class EtudeState_Localizer {
    public static string GetLocalized(this EtudeState type) {
        return type switch {
            EtudeState.NotStarted => m_NotStartedLocalizedText,
            EtudeState.Started => m_StartedLocalizedText,
            EtudeState.Active => m_ActiveLocalizedText,
            EtudeState.PreCompleted => m_PreCompletedLocalizedText,
            EtudeState.Completed => m_CompletedLocalizedText,
            EtudeState.CompletionInProgress => m_CompletionInProgressLocalizedText,
            _ => "!!Error Unknown EtudeState!!",
        };
    }

    [LocalizedString("ToyBox_Infrastructure_Enums_EtudeState_Localizer_m_NotStartedLocalizedText", "Not Started")]
    private static partial string m_NotStartedLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_EtudeState_Localizer_m_StartedLocalizedText", "Started")]
    private static partial string m_StartedLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_EtudeState_Localizer_m_ActiveLocalizedText", "Active")]
    private static partial string m_ActiveLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_EtudeState_Localizer_m_PreCompletedLocalizedText", "Pre Completed")]
    private static partial string m_PreCompletedLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_EtudeState_Localizer_m_CompletedLocalizedText", "Completed")]
    private static partial string m_CompletedLocalizedText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_EtudeState_Localizer_m_CompletionInProgressLocalizedText", "Completion in Progress")]
    private static partial string m_CompletionInProgressLocalizedText { get; }
}
