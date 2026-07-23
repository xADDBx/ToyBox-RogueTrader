using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Colonization;
using ToyBox.Infrastructure.Inspector;
using UnityEngine;

namespace ToyBox.Features.Colonies;

public partial class ColonyEditorFeature : Feature {
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_Name", "Colony Editor")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_Description", "View and edit your colonies: their stats, traits, ongoing events and projects, and the shared resource pool.")]
    public override partial string Description { get; }

    private enum ColonyStatKind { Contentment, Efficiency, Security }

    private ColoniesState.ColonyData? m_SelectedColony;
    private ColonyStatKind m_SelectedStat = ColonyStatKind.Contentment;
    private int m_StatAdjustment = 1;
    private int m_ResourceAdjustment = 10;
    private bool m_EditResources = false;

    private Browser<BlueprintColonyTrait>? m_TraitBrowser;
    private Colony? m_TraitBrowserColony;
    private Browser<BlueprintResource>? m_ResourceBrowser;
    private bool m_ResourcesRequested = false;

    public override void Enable() {
        Main.OnLocaleChanged += ClearCaches;
        Main.OnHideGUIAction += ClearCaches;
    }

    public override void Disable() {
        Main.OnLocaleChanged -= ClearCaches;
        Main.OnHideGUIAction -= ClearCaches;
    }

    private void ClearCaches() {
        m_TraitBrowser = null;
        m_TraitBrowserColony = null;
        m_ResourceBrowser = null;
        m_ResourcesRequested = false;
    }

    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        var colonies = Game.Instance?.Player?.ColoniesState?.Colonies;
        if (colonies == null || colonies.Count == 0) {
            UI.Label(m_NoColoniesText.Orange());
            return;
        }
        if (m_SelectedColony == null || !colonies.Contains(m_SelectedColony)) {
            m_SelectedColony = colonies[0];
        }

        using (HorizontalScope()) {
            using (VerticalScope(GUI.skin.box, Width(320 * Main.UIScale))) {
                UI.Label(m_ColoniesText.Cyan().Bold());
                _ = UI.SelectionGrid(ref m_SelectedColony, colonies, 1, cd => BPHelper.GetTitle(cd.Colony.Blueprint), Width(300 * Main.UIScale));
            }
            Space(10);
            using (VerticalScope()) {
                ColonyGUI(m_SelectedColony.Colony);
            }
        }

        Div.DrawDiv();
        _ = UI.DisclosureToggle(ref m_EditResources, m_EditResourcesText.Cyan().Bold());
        if (m_EditResources) {
            ResourcesGUI();
        }
    }

    private void ColonyGUI(Colony colony) {
        UI.Label(m_OngoingEventsText.Cyan().Bold());
        using (HorizontalScope()) {
            Space(25);
            using (VerticalScope()) {
                if (colony.StartedEvents.Count == 0) {
                    UI.Label(SharedStrings.NoneText.Grey());
                } else {
                    foreach (var evt in colony.StartedEvents) {
                        UI.Label(BPHelper.GetTitle(evt).Orange(), Width(500));
                    }
                }
            }
        }

        UI.Label(m_OngoingProjectsText.Cyan().Bold());
        using (HorizontalScope()) {
            Space(25);
            using (VerticalScope()) {
                var activeProjects = colony.Projects.Where(p => !p.IsFinished).ToList();
                if (activeProjects.Count == 0) {
                    UI.Label(SharedStrings.NoneText.Grey());
                } else {
                    foreach (var proj in activeProjects) {
                        using (HorizontalScope()) {
                            UI.Label(BPHelper.GetTitle(proj.Blueprint).Orange(), Width(500));
                            try {
                                _ = UI.Button(m_FinishText.Cyan(), () => colony.FinishProject(proj), null, AutoWidth());
                            } catch (Exception ex) {
                                // One of the subscribers throws. I think that's caught already but I'll catch just in case
                                Debug(ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        using (HorizontalScope()) {
            UI.Label(m_ContentmentText.Cyan() + ": " + colony.Contentment.Value.ToString().Orange(), Width(220));
            UI.Label(m_EfficiencyText.Cyan() + ": " + colony.Efficiency.Value.ToString().Orange(), Width(220));
            UI.Label(m_SecurityText.Cyan() + ": " + colony.Security.Value.ToString().Orange(), Width(220));
        }
        using (HorizontalScope()) {
            UI.Label(m_ChangeStatText.Cyan(), Width(220));
            _ = UI.SelectionGrid(ref m_SelectedStat, 3, StatTitle, Width(500));
        }
        using (HorizontalScope()) {
            UI.Label((m_AdjustByText + ":").Cyan(), AutoWidth());
            Space(10);
            _ = UI.TextField(ref m_StatAdjustment, null, Width(150));
            m_StatAdjustment = Math.Max(0, m_StatAdjustment);
            Space(10);
            _ = UI.Button(m_AddText.Cyan(), () => CheatsColonization.AddColonyStat(colony.Blueprint, StatKey(m_SelectedStat), m_StatAdjustment), null, AutoWidth());
            Space(10);
            _ = UI.Button(m_RemoveText.Cyan(), () => CheatsColonization.AddColonyStat(colony.Blueprint, StatKey(m_SelectedStat), -m_StatAdjustment), null, AutoWidth());
        }

        Space(10);
        UI.Label(m_TraitsText.Cyan().Bold());
        m_TraitBrowser ??= new(BPHelper.GetSortKey, BPHelper.GetSearchKey, showAllFunc: func => { _ = BPLoader.GetBlueprintsOfType<BlueprintColonyTrait>(func); });
        if (m_TraitBrowserColony != colony) {
            m_TraitBrowserColony = colony;
            m_TraitBrowser.UpdateItems(colony.ColonyTraits.Keys);
        }
        m_TraitBrowser.OnGUI(trait => TraitRowGUI(colony, trait), TraitHeaderGUI);
    }

    private void TraitHeaderGUI() {
        using (HorizontalScope()) {
            _ = UI.Toggle(m_ShowGuidsText.Cyan(), null, ref Settings.ShowAssetIDs);
        }
    }

    private void TraitRowGUI(Colony colony, BlueprintColonyTrait trait) {
        var isAdded = colony.ColonyTraits.ContainsKey(trait);
        using (HorizontalScope()) {
            if (isAdded) {
                _ = UI.Button(m_RemoveText.Cyan(), () => {
                    colony.RemoveTrait(trait);
                    Main.ScheduleForMainThread(() => m_TraitBrowser?.UpdateItems(colony.ColonyTraits.Keys));
                }, null, Width(150));
            } else {
                _ = UI.Button(m_AddText.Cyan(), () => {
                    colony.AddTrait(trait);
                    Main.ScheduleForMainThread(() => m_TraitBrowser?.UpdateItems(colony.ColonyTraits.Keys));
                }, null, Width(150));
            }
            Space(10);
            var title = BPHelper.GetTitle(trait);
            UI.Label(isAdded ? title.Cyan().Bold() : title.Orange(), Width(400));
            Space(10);
            InspectorUI.InspectToggle(trait, m_InspectText.Cyan(), options: AutoWidth());
            if (Settings.ShowAssetIDs) {
                var guid = trait.AssetGuid.ToString();
                Space(5);
                _ = UI.TextField(ref guid, null, Width(300));
            }
            Space(10);
            UI.Label((BPHelper.GetDescription(trait) ?? "").Green(), GUILayout.ExpandWidth(true));
        }
        InspectorUI.InspectIfExpanded(trait);
    }

    private void ResourcesGUI() {
        m_ResourceBrowser ??= new(BPHelper.GetSortKey, BPHelper.GetSearchKey);
        if (!m_ResourcesRequested) {
            m_ResourcesRequested = true;
            _ = BPLoader.GetBlueprintsOfType<BlueprintResource>(bps => Main.ScheduleForMainThread(() => m_ResourceBrowser?.UpdateItems(bps)));
        }
        using (HorizontalScope()) {
            UI.Label((m_ResourceAdjustText + ":").Cyan(), AutoWidth());
            Space(10);
            _ = UI.TextField(ref m_ResourceAdjustment, null, Width(150));
        }
        m_ResourceBrowser.OnGUI(ResourceRowGUI);
    }

    private void ResourceRowGUI(BlueprintResource resource) {
        using (HorizontalScope()) {
            _ = UI.Button(m_AddText.Cyan(), () => Game.Instance.ColonizationController.AddResourceNotFromColonyToPool(resource, m_ResourceAdjustment), null, Width(120));
            Space(5);
            _ = UI.Button(m_RemoveText.Cyan(), () => Game.Instance.ColonizationController.UseResourceFromPool(resource, m_ResourceAdjustment), null, Width(120));
            Space(10);
            UI.Label(BPHelper.GetTitle(resource).Orange(), Width(350));
            Space(10);
            var amount = Game.Instance.Player.ColoniesState.ResourcesNotFromColonies.TryGetValue(resource, out var a) ? a : 0;
            UI.Label((m_CurrentAmountText + ": ").Cyan() + amount.ToString().Orange(), Width(150));
            Space(10);
            InspectorUI.InspectToggle(resource, m_InspectText.Cyan(), options: AutoWidth());
            Space(10);
            UI.Label((BPHelper.GetDescription(resource) ?? "").Green(), GUILayout.ExpandWidth(true));
        }
        InspectorUI.InspectIfExpanded(resource);
    }

    private static string StatKey(ColonyStatKind kind) {
        return kind switch {
            ColonyStatKind.Contentment => "contentment",
            ColonyStatKind.Efficiency => "efficiency",
            ColonyStatKind.Security => "security",
            _ => "",
        };
    }

    private static string StatTitle(ColonyStatKind kind) {
        return kind switch {
            ColonyStatKind.Contentment => m_ContentmentText,
            ColonyStatKind.Efficiency => m_EfficiencyText,
            ColonyStatKind.Security => m_SecurityText,
            _ => kind.ToString(),
        };
    }

    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_NoColoniesText", "You have not founded any colonies yet.")]
    private static partial string m_NoColoniesText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_ColoniesText", "Colonies")]
    private static partial string m_ColoniesText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_OngoingEventsText", "Ongoing Events")]
    private static partial string m_OngoingEventsText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_OngoingProjectsText", "Ongoing Projects")]
    private static partial string m_OngoingProjectsText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_FinishText", "Finish")]
    private static partial string m_FinishText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_ContentmentText", "Contentment")]
    private static partial string m_ContentmentText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_EfficiencyText", "Efficiency")]
    private static partial string m_EfficiencyText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_SecurityText", "Security")]
    private static partial string m_SecurityText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_ChangeStatText", "Change Colony Stat")]
    private static partial string m_ChangeStatText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_AdjustByText", "Adjust the selected stat by this amount")]
    private static partial string m_AdjustByText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_AddText", "Add")]
    private static partial string m_AddText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_RemoveText", "Remove")]
    private static partial string m_RemoveText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_TraitsText", "Colony Traits")]
    private static partial string m_TraitsText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_ShowGuidsText", "Show GUIDs")]
    private static partial string m_ShowGuidsText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_InspectText", "Inspect")]
    private static partial string m_InspectText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_EditResourcesText", "Edit Resources")]
    private static partial string m_EditResourcesText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_ResourceAdjustText", "Adjust resource amount by")]
    private static partial string m_ResourceAdjustText { get; }
    [LocalizedString("ToyBox_Features_Colonies_ColonyEditorFeature_m_CurrentAmountText", "Current")]
    private static partial string m_CurrentAmountText { get; }
}
