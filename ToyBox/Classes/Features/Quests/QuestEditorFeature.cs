using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using ToyBox.Infrastructure.Inspector;
using UnityEngine;

namespace ToyBox.Features.Quests;

public partial class QuestEditorFeature : Feature {
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_Name", "Quest Editor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_Description", "Browse your quests and objectives and force their state: start or complete objectives, restart failed ones, etc.")]
    public override partial string Description { get; }

    private readonly HashSet<Quest> m_Expanded = [];

    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        var quests = Game.Instance?.Player?.QuestBook?.Quests?.ToList();
        if (quests == null || quests.Count == 0) {
            UI.Label(m_NoQuestsText.Orange());
            return;
        }

        using (HorizontalScope()) {
            _ = UI.Toggle(m_HideCompletedText, null, ref Settings.QuestsHideCompleted);
            Space(25);
            _ = UI.Toggle(m_ShowUnrevealedText, null, ref Settings.QuestsShowUnrevealedObjectives);
            Space(25);
            _ = UI.Toggle(m_ShowInspectorText, null, ref Settings.QuestsShowInspector);
        }
        Div.DrawDiv();

        foreach (var quest in quests.OrderBy(q => q.State == QuestState.Completed)) {
            if (Settings.QuestsHideCompleted && quest.State == QuestState.Completed) {
                continue;
            }
            QuestGUI(quest);
        }
        Space(25);
    }

    private void QuestGUI(Quest quest) {
        var expanded = m_Expanded.Contains(quest);
        using (HorizontalScope()) {
            Space(25);
            var title = ColorByState(quest.Blueprint.Title.Text, quest.State).Bold();
            if (UI.DisclosureToggle(ref expanded, title, options: Width(600 * Main.UIScale))) {
                _ = expanded ? m_Expanded.Add(quest) : m_Expanded.Remove(quest);
            }
            Space(25);
            UI.Label(StateString(quest.State), Width(150 * Main.UIScale));
            if (Settings.QuestsShowInspector) {
                InspectorUI.InspectToggle(quest, m_InspectText.Cyan(), options: AutoWidth());
            }
            Space(25);
            UI.Label(StripHTML(quest.Blueprint.Description.Text).Green(), GUILayout.ExpandWidth(true));
        }
        if (Settings.QuestsShowInspector) {
            InspectorUI.InspectIfExpanded(quest);
        }
        if (m_Expanded.Contains(quest)) {
            var objectiveIndex = 0;
            foreach (var objective in quest.Objectives) {
                if (objective.ParentObjective != null || (!Settings.QuestsShowUnrevealedObjectives && !IsRevealed(objective))) {
                    continue;
                }
                objectiveIndex++;
                ObjectiveGUI(quest, objective, objectiveIndex, 75);
                if (objective.State == QuestObjectiveState.Started) {
                    var childIndex = 0;
                    foreach (var child in quest.Objectives) {
                        if (child.ParentObjective != objective) {
                            continue;
                        }
                        if (!Settings.QuestsShowUnrevealedObjectives && !IsRevealed(child)) {
                            continue;
                        }
                        childIndex++;
                        ObjectiveGUI(quest, child, childIndex, 125);
                    }
                }
            }
        }
        Div.DrawDiv();
    }

    private void ObjectiveGUI(Quest quest, QuestBookEntityEntry objective, int index, float indent) {
        using (HorizontalScope()) {
            Space(indent);
            UI.Label(index.ToString().Grey(), Width(40));
            UI.Label(ObjectiveTitle(objective), Width(550 * Main.UIScale));
            Space(25);
            UI.Label(StateString(objective.State), Width(150 * Main.UIScale));
            if (Settings.QuestsShowInspector) {
                InspectorUI.InspectToggle(objective, m_InspectText.Cyan(), options: AutoWidth());
            }
            Space(25);
            ObjectiveActionsGUI(quest, objective);
            Space(25);
            UI.Label(StripHTML(objective.Blueprint.Description.Text).Green(), GUILayout.ExpandWidth(true));
        }
        if (Settings.QuestsShowInspector) {
            InspectorUI.InspectIfExpanded(objective);
        }
    }

    private void ObjectiveActionsGUI(Quest quest, QuestBookEntityEntry objective) {
        switch (objective.State) {
            case QuestObjectiveState.None: {
                    _ = UI.Button(m_StartText.Cyan(), () => objective.Start(), null, Width(150));
                }
                break;
            case QuestObjectiveState.Started: {
                    _ = UI.Button((objective.Blueprint.IsFinishParent ? m_FinishText : m_CompleteText).Cyan(), () => objective.Complete(), null, Width(150));
                    if (objective.Blueprint.AutoFailDays > 0) {
                        Space(5);
                        _ = UI.Button(m_ResetTimeText.Cyan(), () => objective.m_ObjectiveStartTime = Game.Instance.Player.GameTime, null, Width(150));
                    }
                }
                break;
            case QuestObjectiveState.Failed: {
                    _ = UI.Button(m_RestartText.Cyan(), () => {
                        if (quest.State is QuestState.Completed or QuestState.Failed) {
                            quest.m_State = QuestState.Started;
                        }
                        objective.Reset();
                        objective.Start();
                    }, null, Width(150));
                }
                break;
            case QuestObjectiveState.Completed:
            case QuestObjectiveState.Postponed:
            default:
                Space(150);
                break;
        }
    }

    private static bool IsRevealed(QuestBookEntityEntry objective) {
        return objective.State is QuestObjectiveState.Started or QuestObjectiveState.Completed;
    }

    private static string ObjectiveTitle(QuestBookEntityEntry objective) {
        var title = objective.Blueprint.Title.Text;
        if (string.IsNullOrEmpty(title)) {
            title = objective.Blueprint.name;
        }
        if (objective.Blueprint.IsAddendum) {
            title = (m_AddendumText + ": ").White() + title;
        }
        return ColorByState(title, objective.State);
    }

    private static string StateString(QuestState state) {
        return state == QuestState.None ? "" : ColorByState(state.ToString(), state).Bold();
    }

    private static string StateString(QuestObjectiveState state) {
        return state == QuestObjectiveState.None ? "" : ColorByState(state.ToString(), state).Bold();
    }

    private static string ColorByState(string text, QuestState state) {
        return state switch {
            QuestState.None => text.Grey(),
            QuestState.Started => text.Cyan(),
            QuestState.Completed => text.White(),
            QuestState.Updated => text.Yellow(),
            QuestState.Postponed => text.Green(),
            QuestState.Failed => text.Red(),
            _ => text,
        };
    }

    private static string ColorByState(string text, QuestObjectiveState state) {
        return state switch {
            QuestObjectiveState.None => text.Grey(),
            QuestObjectiveState.Started => text.Cyan(),
            QuestObjectiveState.Completed => text.White(),
            QuestObjectiveState.Postponed => text.Green(),
            QuestObjectiveState.Failed => text.Red(),
            _ => text,
        };
    }

    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_NoQuestsText", "You have no quests in your quest book yet.")]
    private static partial string m_NoQuestsText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_HideCompletedText", "Hide Completed")]
    private static partial string m_HideCompletedText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_ShowUnrevealedText", "Show Unrevealed Steps")]
    private static partial string m_ShowUnrevealedText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_ShowInspectorText", "Inspect Quests and Objectives")]
    private static partial string m_ShowInspectorText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_InspectText", "Inspect")]
    private static partial string m_InspectText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_StartText", "Start")]
    private static partial string m_StartText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_CompleteText", "Complete")]
    private static partial string m_CompleteText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_FinishText", "Finish")]
    private static partial string m_FinishText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_ResetTimeText", "Reset Time")]
    private static partial string m_ResetTimeText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_RestartText", "Restart")]
    private static partial string m_RestartText { get; }
    [LocalizedString("ToyBox_Features_Quests_QuestEditorFeature_m_AddendumText", "Addendum")]
    private static partial string m_AddendumText { get; }
}
