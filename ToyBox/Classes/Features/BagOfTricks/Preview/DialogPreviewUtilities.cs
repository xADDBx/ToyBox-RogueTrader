using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Alignments;

namespace ToyBox.Features.BagOfTricks.Preview;

public static partial class DialogPreviewUtilities {
    internal const string Indent = "        ";
    private const int m_MaxDepth = 20;
    private static List<(BlueprintCueBase?, int, GameAction[]?, SoulMarkShift?, SoulMarkShift?)> CollateAnswerData(BlueprintAnswer answer, out bool isRecursive) {
        var cueResults = new List<(BlueprintCueBase?, int, GameAction[]?, SoulMarkShift?, SoulMarkShift?)>();
        var toCheck = new Queue<(BlueprintCueBase, int)>();
        isRecursive = false;
        var visited = new HashSet<BlueprintAnswerBase> {
            answer
        };
        if (answer.NextCue.Cues.Count > 0) {
            toCheck.Enqueue((answer.NextCue.Select(), 1));
        }
        cueResults.Add((null, 0, answer.OnSelect.Actions, answer.SoulMarkShift, answer.SoulMarkRequirement));
        while (toCheck.Count > 0) {
            var item = toCheck.Dequeue();
            var cueBase = item.Item1;
            var currentDepth = item.Item2;
            if (currentDepth > m_MaxDepth) {
                break;
            }

            if (cueBase is BlueprintCue cue) {
                cueResults.Add((cue, currentDepth, [.. cue.OnShow.Actions, .. cue.OnStop.Actions], cue.SoulMarkShift, cue.SoulMarkRequirement));
                if (cue.Answers.Count > 0) {
                    var subAnswer = cue.Answers[0].Get();
                    if (visited.Contains(subAnswer)) {
                        isRecursive = true;
                        break;
                    }
                    _ = visited.Add(subAnswer);
                }
                if (cue.Continue.Cues.Count > 0) {
                    toCheck.Enqueue((cue.Continue.Select(), currentDepth + 1));
                }
            } else if (cueBase is BlueprintBookPage page) {
                cueResults.Add((page, currentDepth, page.OnShow.Actions, null, null));
                if (page.Answers.Count > 0) {
                    var subAnswer = page.Answers[0].Get();
                    if (visited.Contains(subAnswer)) {
                        isRecursive = true;
                        break;
                    }
                    _ = visited.Add(subAnswer);
                    if (page.Answers[0].Get() is BlueprintAnswersList) {
                        break;
                    }
                }
                if (page.Cues.Count > 0) {
                    foreach (var c in page.Cues) {
                        var canShow = false;
                        try {
                            canShow = c.Get().CanShow();
                        } catch (Exception ex) {
                            Warn($"Answer Preview caught exception:\n{ex}");
                        }
                        if (canShow) {
                            toCheck.Enqueue((c, currentDepth + 1));
                        }
                    }
                }
            } else if (cueBase is BlueprintCheck check) {
                toCheck.Enqueue((check.Success, currentDepth + 1));
                toCheck.Enqueue((check.Fail, currentDepth + 1));
            } else if (cueBase is BlueprintCueSequence sequence) {
                foreach (var c in sequence.Cues) {
                    var canShow = false;
                    try {
                        canShow = c.Get().CanShow();
                    } catch (Exception ex) {
                        Warn($"Answer Preview caught exception:\n{ex}");
                    }
                    if (canShow) {
                        toCheck.Enqueue((c, currentDepth + 1));
                    }
                }
                if (sequence.Exit != null) {
                    var exit = sequence.Exit;
                    if (exit.Answers.Count > 0) {
                        var subAnswer = exit.Answers[0].Get();
                        if (visited.Contains(subAnswer)) {
                            isRecursive = true;
                            break;
                        }
                        _ = visited.Add(subAnswer);
                        if (exit.Continue.Cues.Count > 0) {
                            toCheck.Enqueue((exit.Continue.Select(), currentDepth + 1));
                        }
                    }
                }
            } else {
                break;
            }
        }
        return cueResults;
    }
    public static string? FormatSoulmarkShift(SoulMarkShift? shift, string format) {
        if (shift != null && shift.Value != 0) {
            if (shift.Description?.Text is string { Length: > 0 } description) {
                return string.Format(format, $"{UIUtility.GetSoulMarkDirectionText(shift.Direction)}, {shift.Value}, {description}");
            }
            return string.Format(format, $"{UIUtility.GetSoulMarkDirectionText(shift.Direction)}, {shift.Value}");
        }
        return null;
    }
    public static string GetAnswerResultText(BlueprintAnswer answer) {
        var answerData = CollateAnswerData(answer, out var isRecursive);
        var text = isRecursive ? "\n" + Indent + "<size=65%>[Repeats]</size>" : "";
        var results = new List<string>();
        foreach (var data in answerData) {
            var cue = data.Item1;
            var depth = data.Item2;
            var actions = data.Item3;
            var alignment = data.Item4;
            var alignmentRequirement = data.Item5;
            var line = new List<string>();
            if ((actions?.Length ?? 0) > 0) {
                line.AddRange(actions.SelectMany(FormatActionAsList));
            }
            if (FormatSoulmarkShift(alignmentRequirement, m_SoulMarkRequiredLocalizedText + "({0])") is { } soulMarkRequiredText) {
                line.Add(soulMarkRequiredText);
            }
            if (FormatSoulmarkShift(alignment, m_SoulMarkShiftLocalizedText + "({0})") is { } soulMarkShiftText) {
                line.Add(soulMarkShiftText);
            }
            if (cue is BlueprintCheck check) {
                line.Add(string.Format(m_Check__1__DC_2__Hidden_3__LocalizedText, check.Type, check.DC, check.Hidden));
            }
            if (line.Count > 0) {
                results.Add($"\n" + Indent + $"[{depth}: {line.Join()}]");
            }
        }
        if (results.Count > 0) {
            text = $"<size=65%>{results.Join(null, "")}</size>";
        }

        return text;
    }
    public static string GetCueResultText(BlueprintCue cue) {
        var actions = cue.OnShow.Actions.Concat(cue.OnStop.Actions).ToArray();
        var alignment = cue.SoulMarkShift;
        var text = "";
        if (actions.Length > 0) {
            var result = FormatActions(actions);
            if (string.IsNullOrWhiteSpace(result)) {
                result = m_EmptyActionLocalizedText;
            }
            text += $" \n<size=65%>[{result}]</size>";
        }
        if (alignment != null && alignment.Value > 0) {
            text += " \n<size=65%>[" + string.Format(m_SoulMarkShift_0_By_1__DescriptioLocalizedText, UIUtility.GetSoulMarkDirectionText(alignment.Direction).Text, alignment.Value, alignment.Description.Text) + "]</size>";
        }
        return text;
    }
    public static List<string> FormatActionAsList(GameAction? action) {
        string caption;
        if (action is Conditional conditional) {
            var actionList = conditional.ConditionsChecker.Check() ? conditional.IfTrue : conditional.IfFalse;
            return [.. actionList.Actions.SelectMany(FormatActionAsList)];
        } else if (action is RunActionHolder actionHolder && actionHolder.Holder.Get()?.Actions is { } subActions) {
            var subActionList = FormatActions(subActions.Actions);
            caption = m_RunActionHolderLocalizedText + $" ({string.Join(", ", subActionList)})";
        } else {
            caption = action?.GetCaption() ?? "";
        }
        if (string.IsNullOrWhiteSpace(caption)) {
            return [action?.GetType().Name];
        } else {
            return [caption];
        }
    }
    public static string FormatActions(GameAction[]? actions) {
        if (actions == null) {
            return "";
        }
        return actions.SelectMany(FormatActionAsList).Join();
    }

    public static string FormatConditions(Condition[]? conditions) {
        if (conditions == null) {
            return "";
        }
        return conditions.Select(c => {
            if (c is CheckConditionsHolder holder) {
                return m_ConditionsHolderLocalizedText + $" ({FormatConditions(holder.ConditionsHolder?.Get()?.Conditions?.Conditions)})";
            } else {
                return c?.GetCaption();
            }
        }).Where(s => !string.IsNullOrWhiteSpace(s)).Join();
    }
    public static List<string> FormatConditionsAsList(BlueprintAnswer answer) {
        var list = new List<string>();
        if (answer.HasShowCheck) {
            list.Add(string.Format(m_ShowCheck__0_DC__1__LocalizedText, answer.ShowCheck.Type, answer.ShowCheck.DC));
        }
        if (answer.ShowConditions?.Conditions?.Length > 0) {
            list.Add(m_ShowConditionsLocalizedText + $" ({FormatConditions(answer.ShowConditions.Conditions)})");
        }

        if (answer.SelectConditions is ConditionsChecker selectChecker && selectChecker.Conditions.Length > 0) {
            list.Add(m_SelectConditionsLocalizedText + $" ({FormatConditions(selectChecker.Conditions)})");
        }

        ;
        return list;
    }

    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_RunActionHolderLocalizedText", "RunActionHolder")]
    private static partial string m_RunActionHolderLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_ConditionsHolderLocalizedText", "Conditions Holder")]
    private static partial string m_ConditionsHolderLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_ShowCheck__0_DC__1__LocalizedText", "Show Check ({0} DC: {1})")]
    private static partial string m_ShowCheck__0_DC__1__LocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_ShowConditionsLocalizedText", "Show Conditions")]
    private static partial string m_ShowConditionsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_SelectConditionsLocalizedText", "Select Conditions")]
    private static partial string m_SelectConditionsLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_EmptyActionLocalizedText", "Empty Action")]
    private static partial string m_EmptyActionLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_SoulMarkShift_0_By_1__DescriptioLocalizedText", "SoulMarkShift {0} by {1} - Description: {2}")]
    private static partial string m_SoulMarkShift_0_By_1__DescriptioLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_SoulMarkRequiredLocalizedText", "SoulMarkRequired")]
    private static partial string m_SoulMarkRequiredLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_SoulMarkShiftLocalizedText", "SoulMarkShift")]
    private static partial string m_SoulMarkShiftLocalizedText { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Preview_DialogPreviewUtilities_m_Check__1__DC_2__Hidden_3__LocalizedText", "Check({1}, DC {2}, hidden {3})")]
    private static partial string m_Check__1__DC_2__Hidden_3__LocalizedText { get; }
}
