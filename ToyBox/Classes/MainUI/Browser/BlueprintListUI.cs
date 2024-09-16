// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
using Kingmaker.Utility;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;
using static ToyBox.BlueprintExtensions;

namespace ToyBox {
    public class BlueprintListUI {
        public delegate void NavigateTo(params string[] argv);
        public static Settings Settings => Main.Settings;

        public static int repeatCount = 1;
        public static bool hasRepeatableAction = false;
        public static int maxActions = 0;
        public static bool needsLayout = true;
        public static int[] ParamSelected = new int[1000];
        public static Dictionary<BlueprintFeatureSelection_Obsolete, string[]> selectionBPValuesNames = new() { };

        public static List<Action> OnGUI(BaseUnitEntity unit,
            IEnumerable<SimpleBlueprint> blueprints,
            float indent = 0, float remainingWidth = 0,
            Func<string, string?>? titleFormatter = null,
            NamedTypeFilter? typeFilter = null,
            NavigateTo? navigateTo = null
        ) {
            List<Action> todo = new();
            if (titleFormatter == null) titleFormatter = (t) => RichText.Bold(RichText.Orange(t));
            if (remainingWidth == 0) remainingWidth = ummWidth - indent;
            var index = 0;
            IEnumerable<SimpleBlueprint> simpleBlueprints = blueprints.ToList();
            if (needsLayout) {
                foreach (var blueprint in simpleBlueprints) {
                    var actions = blueprint.GetActions();
                    if (actions.Any(a => a.isRepeatable)) hasRepeatableAction = true;

                    // FIXME - perf bottleneck 
                    var actionCount = actions.Sum(action => action.canPerform(blueprint, unit) ? 1 : 0);
                    maxActions = Math.Max(actionCount, maxActions);
                }
                needsLayout = false;
            }
            if (hasRepeatableAction) {
                BeginHorizontal();
                Label("", MinWidth(350 - indent), MaxWidth(600));
                ActionIntTextField(
                    ref repeatCount,
                    "repeatCount",
                    (limit) => { },
                    () => { },
                    Width(160));
                Space(40);
                Label(RichText.Cyan("Parameter") + ": " + RichText.Orange($"{repeatCount}"), ExpandWidth(false));
                repeatCount = Math.Max(1, repeatCount);
                repeatCount = Math.Min(100, repeatCount);
                EndHorizontal();
            }
            Div(indent);
            var count = 0;
            foreach (var blueprint in simpleBlueprints) {
                var currentCount = count++;
                var description = blueprint.GetDescription().MarkedSubstring(Settings.searchText);
                if (blueprint is BlueprintItem itemBlueprint && itemBlueprint.FlavorText?.Length > 0)
                    description = $"{itemBlueprint.FlavorText.StripHTML().Color(RGBA.notable).MarkedSubstring(Settings.searchText)}\n{description}";
                float titleWidth = 0;
                var remWidth = remainingWidth - indent;
                using (HorizontalScope()) {
                    Space(indent);
                    var actions = blueprint.GetActions()
                        .Where(action => action.canPerform(blueprint, unit));
                    var titles = actions.Select(a => a.name);
                    var name = GetTitle(blueprint);
                    var displayName = blueprint.GetDisplayName();
                    string? title;
                    if (Settings.showDisplayAndInternalNames && displayName.Length > 0 && displayName != name) {
                        // FIXME - horrible perf bottleneck 
                        if (titles.Contains("Remove".localize()) || titles.Contains("Lock".localize())) {
                            title = RichText.Bold(RichText.Cyan(displayName));
                        } else {
                            title = titleFormatter(displayName);
                        }
                        title = $"{title} : {name.Color(RGBA.darkgrey)}";
                    } else {
                        // FIXME - horrible perf bottleneck 
                        if (titles.Contains("Remove".localize()) || titles.Contains("Lock".localize())) {
                            title = RichText.Bold(RichText.Cyan(name));
                        } else {
                            title = titleFormatter(name);
                        }
                    }
                    titleWidth = (remainingWidth / (IsWide ? 3 : 4)) - indent;
                    Label(title.MarkedSubstring(Settings.searchText), Width(titleWidth));
                    remWidth -= titleWidth;

                    // FIXME - perf bottleneck 
                    var actionCount = actions != null ? actions.Count() : 0;

                    // FIXME - perf bottleneck 
                    var lockIndex = titles.IndexOf("Lock");
                    if (blueprint is BlueprintUnlockableFlag flagBP) {
                        // special case this for now
                        if (lockIndex >= 0) {
                            var flags = Game.Instance.Player.UnlockableFlags;
                            var lockAction = actions.ElementAt(lockIndex);
                            ActionButton("<", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) - 1); }, Width(50));
                            Space(25);
                            Label(RichText.Bold(RichText.Orange($"{flags.GetFlagValue(flagBP)}")), MinWidth(50));
                            ActionButton(">", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) + 1); }, Width(50));
                            Space(50);
                            ActionButton(lockAction.name, () => { lockAction.action(blueprint, unit, repeatCount); }, Width(120));
                            Space(100);
#if DEBUG
                            Label(flagBP.GetDescription().green());
#endif
                        } else {
                            // FIXME - perf bottleneck 
                            var unlockIndex = titles.IndexOf("Unlock");
                            if (unlockIndex >= 0) {
                                var unlockAction = actions.ElementAt(unlockIndex);
                                Space(240);
                                ActionButton(unlockAction.name, () => { unlockAction.action(blueprint, unit, repeatCount); }, Width(120));
                                Space(100);
                            }
                        }
                        remWidth -= 300;
                    } else {
                        for (var ii = 0; ii < maxActions; ii++) {
                            if (ii < actionCount) {
                                var action = actions.ElementAt(ii);
                                // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                                if (action.name == "<" || action.name == ">") {
                                    Space(174); continue;
                                }
                                var actionName = action.name;
                                float extraSpace = 0;
                                if (action.isRepeatable) {
                                    actionName += action.isRepeatable ? $" {repeatCount}" : "";
                                    extraSpace = 20 * (float)Math.Ceiling(Math.Log10((double)repeatCount));
                                }
                                ActionButton(actionName, () => todo.Add(() => action.action(blueprint, unit, repeatCount, currentCount)), Width(160 + extraSpace));
                                Space(10);
                                remWidth -= 174.0f + extraSpace;

                            } else {
                                Space(174);
                            }
                        }
                    }
                    Space(10);
                    var typeString = blueprint.GetType().Name;
                    var navigateStrings = new List<string> { typeString };
                    if (typeFilter?.collator != null) {
                        var names = typeFilter.collator(blueprint);
                        if (names.Count > 0) {
                            var collatorString = names.First();
                            if (blueprint is BlueprintItem itemBP) {
                                var rarity = itemBP.Rarity();
                                typeString = $"{typeString} - {rarity}".Rarity(rarity);
                            }
                            if (!typeString.Contains(collatorString)) {
                                typeString += RichText.Yellow($" : {collatorString}");
                                navigateStrings.Add(collatorString);
                            }
                        }
                    }
                    var attributes = "";
                    if (Settings.showAttributes) {
                        var attr = string.Join(" ", blueprint.Attributes());
                        if (!typeString.Contains(attr))
                            attributes = attr;
                    }

                    if (attributes.Length > 1) typeString += $" - {RichText.Orange(attributes)}";

                    if (description != null && description.Length > 0) description = $"{description}";
                    else description = "";
                    if (blueprint is BlueprintScriptableObject bpso) {
                        if (Settings.showComponents && bpso.ComponentsArray?.Length > 0) {
                            var componentStr = string.Join<object>(", ", bpso.ComponentsArray).Color(RGBA.brown);
                            if (description.Length == 0) description = componentStr;
                            else description = description + "\n" + componentStr;
                        }
                        if (Settings.showElements && bpso.ElementsArray?.Count > 0) {
                            var elementsStr = RichText.Yellow(string.Join<object>("\n", bpso.ElementsArray.Select(e => $"{e.GetType().Name.Cyan()} {e.GetCaption()}")));
                            if (description.Length == 0) description = elementsStr;
                            else description = description + "\n" + elementsStr;
                        }
                    }
                    using (VerticalScope(Width(remWidth))) {
                        using (HorizontalScope(Width(remWidth))) {
                            ReflectionTreeView.DetailToggle("", blueprint, blueprint, 0);
                            Space(-17);
                            if (Settings.showAssetIDs) {
                                ActionButton(typeString, () => navigateTo?.Invoke(navigateStrings.ToArray()), rarityButtonStyle);
                                ClipboardLabel(blueprint.AssetGuid.ToString(), ExpandWidth(false));
                            } else ActionButton(typeString, () => navigateTo?.Invoke(navigateStrings.ToArray()), rarityButtonStyle);
                            Space(17);
                        }
                        if (description.Length > 0) Label(RichText.Green(description), Width(remWidth));
                    }
                }
                if (blueprint is BlueprintFeatureSelection_Obsolete selectionBP) {
                    using (HorizontalScope()) {
                        Space(titleWidth);
                        using (VerticalScope()) {
                            var nameStrings = selectionBPValuesNames.GetValueOrDefault(selectionBP, null);
                            if (nameStrings == null) {
                                nameStrings = selectionBP.AllFeatures.Select(x => x.NameSafe()).OrderBy(x => x).ToArray().TrimCommonPrefix();
                                selectionBPValuesNames[selectionBP] = nameStrings;
                            }
                            using (HorizontalScope(GUI.skin.button)) {
                                var content = new GUIContent($"{RichText.Yellow(selectionBP.Name)}");
                                var labelWidth = GUI.skin.label.CalcSize(content).x;
                                Space(indent);
                                //UI.Space(indent + titleWidth - labelWidth - 25);
                                Label(content, Width(labelWidth));
                                Space(25);

                                ActionSelectionGrid(
                                    ref ParamSelected[currentCount],
                                    nameStrings,
                                    4,
                                    (selected) => { },
                                    GUI.skin.toggle,
                                    Width(remWidth)
                                );
                                //UI.SelectionGrid(ref ParamSelected[currentCount], nameStrings, 6, UI.Width(remWidth + titleWidth)); // UI.Width(remWidth));
                            }
                            Space(15);
                        }
                    }
                }
                ReflectionTreeView.OnDetailGUI(blueprint, 0);
                Div(indent);
                index++;
            }
            return todo;
        }
    }
}