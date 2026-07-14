using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using ToyBox.Features.BagOfTricks.Preview;

namespace ToyBox.Features.DialogAndNpc;

public partial class DialogEditorFeature : Feature {
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_Name", "Dialog Editor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_Description", "Shows the structure of the currently active dialog (cues, answers, conditions and results).")]
    public override partial string Description { get; }

    private const int m_Indent = 75;
    private readonly HashSet<BlueprintScriptableObject> m_Visited = [];
    private readonly HashSet<object> m_Expanded = [];

    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        if (Game.Instance?.DialogController is not { } dialogController) {
            return;
        }
        m_Visited.Clear();
        if (dialogController.CurrentCue == null) {
            UI.Label(m_NoActiveDialogText.Cyan());
        } else {
            CueGUI(dialogController.CurrentCue, m_CurrentText);
        }
        var answers = dialogController.Answers?.ToList();
        if (answers is { Count: > 0 }) {
            AnswersGUI(answers, m_AnswerText);
        }
    }

    private bool ExpandToggle(object key, string label) {
        var expanded = m_Expanded.Contains(key);
        if (UI.DisclosureToggle(ref expanded, label)) {
            _ = expanded ? m_Expanded.Add(key) : m_Expanded.Remove(key);
        }
        return m_Expanded.Contains(key);
    }

    private void CueGUI(BlueprintCue cue, string? title = null) {
        var visited = m_Visited.Contains(cue);
        using (HorizontalScope()) {
            TitleGUI(title);
            using (VerticalScope()) {
                var displayText = cue.DisplayText;
                if (visited && displayText.Length > 50) {
                    displayText = StripHTML(displayText).Substring(0, 50) + "...";
                }
                UI.Label($"{cue.name.Yellow()} {displayText.Orange()}");
                var resultsText = StripHTML(DialogPreviewUtilities.GetCueResultText(cue)).Trim();
                if (!string.IsNullOrEmpty(resultsText)) {
                    using (HorizontalScope()) {
                        UI.Label("", Width(m_Indent));
                        UI.Label(resultsText.Yellow());
                    }
                }
                if (cue.Conditions?.Conditions?.Length > 0) {
                    using (HorizontalScope()) {
                        UI.Label(m_CondText.Cyan(), Width(m_Indent));
                        UI.Label(DialogPreviewUtilities.FormatConditions(cue.Conditions.Conditions).Cyan());
                    }
                }
                if (visited) {
                    UI.Label(m_RepeatText.Yellow());
                    return;
                }
                _ = m_Visited.Add(cue);
                var index = 1;
                foreach (var answerBaseRef in cue.Answers) {
                    switch (answerBaseRef.Get()) {
                        case BlueprintAnswer answer: {
                                AnswerGUI(answer, m_AnswerText + $" {index}");
                                index++;
                            }
                            break;
                        case BlueprintAnswersList answersList: {
                                var subIndex = 1;
                                foreach (var subAnswerBaseRef in answersList.Answers) {
                                    if (subAnswerBaseRef.Get() is BlueprintAnswer subAnswer) {
                                        AnswerGUI(subAnswer, $"{index}-{subIndex}");
                                        subIndex++;
                                    }
                                }
                                index++;
                            }
                            break;
                        default:
                            break;
                    }
                }
                if (cue.Continue is { } cueSelection) {
                    CueSelectionGUI(cueSelection, m_SelectionText);
                }
            }
        }
    }

    private void CueSelectionGUI(CueSelection cueSelection, string? title = null) {
        var cues = cueSelection.Cues;
        if (!cues.Any(cbr => cbr.Get() is BlueprintCue)) {
            return;
        }
        using (HorizontalScope()) {
            TitleGUI(title);
            using (VerticalScope()) {
                var index = 1;
                foreach (var cueBaseRef in cues) {
                    if (cueBaseRef.Get() is BlueprintCue cue) {
                        CueGUI(cue, m_CueText + $" {index}");
                        index++;
                    }
                }
            }
        }
    }

    private void AnswerGUI(BlueprintAnswer answer, string? title = null) {
        using (HorizontalScope()) {
            TitleGUI(title);
            using (VerticalScope()) {
                var text = $"{answer.name.Yellow()} {answer.DisplayText}";
                if (answer.NextCue is { } nextCueSelection && nextCueSelection.Cues.Count > 0) {
                    if (ExpandToggle(nextCueSelection, text)) {
                        CueSelectionGUI(nextCueSelection, m_NextText);
                    }
                } else {
                    UI.Label(text);
                }
                foreach (var checkString in DialogPreviewUtilities.FormatConditionsAsList(answer)) {
                    UI.Label(checkString.Cyan());
                }
                var resultsText = StripHTML(DialogPreviewUtilities.GetAnswerResultText(answer));
                if (!string.IsNullOrEmpty(resultsText)) {
                    using (HorizontalScope()) {
                        UI.Label("", Width(m_Indent));
                        UI.Label(resultsText.Yellow());
                    }
                }
            }
        }
    }

    private void AnswersGUI(List<BlueprintAnswer> answers, string? title = null) {
        if (answers.Count == 0) {
            return;
        }
        using (HorizontalScope()) {
            TitleGUI(title);
            using (VerticalScope()) {
                var index = 1;
                foreach (var answer in answers) {
                    AnswerGUI(answer, $"{index}");
                    index++;
                }
            }
        }
    }

    private static void TitleGUI(string? title) {
        if (title != null) {
            UI.Label(title.Cyan(), Width(m_Indent));
        } else {
            Space(m_Indent);
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_NoActiveDialogText", "No Active Dialog")]
    private static partial string m_NoActiveDialogText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_CurrentText", "Current")]
    private static partial string m_CurrentText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_AnswerText", "Answer")]
    private static partial string m_AnswerText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_SelectionText", "Selection")]
    private static partial string m_SelectionText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_CueText", "Cue")]
    private static partial string m_CueText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_NextText", "Next")]
    private static partial string m_NextText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_CondText", "Cond")]
    private static partial string m_CondText { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_DialogEditorFeature_m_RepeatText", "[Repeat]")]
    private static partial string m_RepeatText { get; }
}
