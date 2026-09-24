using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.PC;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Utils;
using Owlcat.Runtime.UI.Controls.Button;
using System.Runtime.CompilerServices;
using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// On-map master toggle (R7): clones the zoom-minus button into the map's
/// right button stack and flips EnhancedMapMaster live.
///
//// BindViewImplementation postfix is installed ONCE at mod load and is never
/// unpatched on user-off. Disable() performs the live-off work WITHOUT
/// base.Disable() so a scene UI rebuild while the master is off can still
/// recreate the button - the orphan-on-area-transition edge cannot occur.
/// Only the auto-disable error path may unpatch; recovery is then the UMM
/// settings toggle or the hotkey.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.EnhancedMapToggleFeature")]
public partial class EnhancedMapToggleFeature : FeatureWithPatch, IToggledWithBinding {
    private const int MaxPatchFailures = 3;
    private const string ButtonName = "ToyBoxEnhancedMapToggle";

    // Distinct tint states: ON = accent yellow, OFF = gray 50%.
    private static readonly Color OnTint = new(1f, 0.85f, 0.25f, 1f);
    private static readonly Color OffTint = new(0.5f, 0.5f, 0.5f, 0.5f);

    private static int s_Failures;
    private static int s_LiveStateFailures;

    private sealed class ButtonRecord {
        public OwlcatButton Button = null!;
        public Image Icon = null!;
        public IDisposable? Hint;
        public IDisposable? Click;
    }

    private static readonly ConditionalWeakTable<LocalMapPCView, ButtonRecord> s_Buttons = new();

    // net481 ConditionalWeakTable has no enumeration; the strong list tracks
    // records for tint refresh. Records die with their persistent view.
    private static readonly List<ButtonRecord> s_AllRecords = new();

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.EnhancedMapToggleFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnhancedMapMaster;
        }
    }

    public Hotkey? Keybind {
        get;
        set;
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_EnhancedMapToggleFeature_Name", "Enhanced Map (Master Toggle)")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_EnhancedMapToggleFeature_Description", "Master switch for the map marker family (NPC dialog markers, exit markers, interactable-object markers, looted-container hiding). An on-map button and a hotkey flip this switch live; nameplates, fonts, fill and rotation are not affected.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_EnhancedMapToggleFeature_m_ButtonTooltipText", "Enhanced Map markers (NPCs and exits)")]
    private static partial string m_ButtonTooltipText { get; }

    // Always patch on load, whatever the persisted master state: the clone is
    // the re-enable path while the map is open.
    public override void Initialize() {
        Enable();
    }

    public override void Enable() {
        if (!IsPatched) {
            ToyBoxPatchCategoryAttribute.PatchCategory(HarmonyName, HarmonyInstance);
            IsPatched = true;
        }
        Keybind ??= Hotkeys.MaybeGetHotkey(GetType());
        // Re-enable resets the strike counters: only consecutive failures may
        // auto-disable.
        s_Failures = 0;
        s_LiveStateFailures = 0;
        ApplyLiveState();
    }

    // User-off: live-off work only; the postfix stays installed (see class
    // comment). base.Disable() (unpatch) is intentionally NOT called.
    public override void Disable() {
        ApplyLiveState();
    }

    public void ExecuteAction(ActionParameter parameter) {
        LogExecution(parameter);
        IsEnabled = !IsEnabled;
        if (IsEnabled) {
            Enable();
        } else {
            Disable();
        }
    }

    public void LogExecution(ActionParameter parameter) {
        Helpers.LogExecution(this, parameter);
    }

    internal static void ApplyLiveState() {
        try {
            RefreshTints();
            if (!TryGetOpenMapVM(out var vm)) {
                return;
            }
            if (NpcDialogClassifier.NpcMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapNpcMarkers)) {
                // Shared append routine ONLY: re-invoking SetMarkers would
                // duplicate every vanilla marker (non-clearing Adds).
                LocalMapNpcMarkersFeature.AppendNpcMarkers(vm);
            } else {
                LocalMapNpcMarkersFeature.RemoveNpcMarkers(vm);
            }
            // The appended exits are OUR VMs (no LocalMapMarkerPart,
            // never touched by the IsVisible postfix), so the master flip
            // must append/remove them live too; vanilla exit VMs un-hide
            // through the postfix on their own. Loot hiding is postfix-live
            // already.
            if (NpcDialogClassifier.ExitMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapExitMarkers)) {
                LocalMapExitMarkersFeature.AppendMarkerlessExitMarkers(vm);
            } else {
                LocalMapExitMarkersFeature.RemoveAppendedExitMarkers(vm);
            }
            // Same appended-VM family for dialog/skill-check objects.
            if (MapObjectMarkerClassifier.InteractableMarkersEffective(Settings.EnhancedMapMaster, Settings.EnableLocalMapInteractableMarkers)) {
                LocalMapInteractableMarkersFeature.AppendInteractableMarkers(vm);
            } else {
                LocalMapInteractableMarkersFeature.RemoveInteractableMarkers(vm);
            }
            if (s_LiveStateFailures > 0) {
                s_LiveStateFailures = 0;
            }
        } catch (Exception ex) {
            // The hotkey can dispatch this every frame while held; a throw
            // here must not spam full-stack logs per frame.
            if (s_LiveStateFailures < MaxPatchFailures) {
                s_LiveStateFailures++;
                Warn($"ToyBox EnhancedMap: live toggle apply failed: {ex.Message}");
            }
        }
    }

    // Each marker feature tracks the open map VM through its own SetMarkers
    // postfix; any live tracker resolves the map (a sub-toggle-OFF feature is
    // unpatched and its tracker stays null).
    private static bool TryGetOpenMapVM(out LocalMapVM vm) {
        return LocalMapNpcMarkersFeature.TryGetCurrentMapVM(out vm)
            || LocalMapExitMarkersFeature.TryGetCurrentMapVM(out vm)
            || LocalMapInteractableMarkersFeature.TryGetCurrentMapVM(out vm);
    }

    private static void RefreshTints() {
        foreach (var record in s_AllRecords) {
            if (record.Icon != null) {
                record.Icon.color = Settings.EnhancedMapMaster ? OnTint : OffTint;
            }
        }
    }

    [HarmonyPatch(typeof(LocalMapPCView), "BindViewImplementation"), HarmonyPostfix]
    private static void LocalMapPCView_BindViewImplementation_Postfix(LocalMapPCView __instance) {
        try {
            // Prune records whose view died (area transitions destroy scene
            // UI): the strong list would otherwise leak for the session.
            s_AllRecords.RemoveAll(record => record.Icon == null);
            var donor = __instance.m_ZoomMinusButton;
            if (donor == null) {
                return;
            }
            _ = s_Buttons.GetValue(__instance, view => CreateButton(view, donor));
            RefreshTints();
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static ButtonRecord CreateButton(LocalMapPCView view, OwlcatButton donor) {
        // The map view GameObject is a persistent serialized child of
        // ServiceWindowsPCView (open/close only SetActive-toggle), so the
        // clone shows/hides with the map for the whole session.
        var clone = UnityEngine.Object.Instantiate(donor.gameObject, donor.transform.parent);
        clone.name = ButtonName;
        clone.transform.SetSiblingIndex(donor.transform.GetSiblingIndex() + 1);
        var button = clone.GetComponent<OwlcatButton>();
        var record = new ButtonRecord {
            Button = button,
            Icon = clone.GetComponentInChildren<Image>(),
        };
        s_AllRecords.Add(record);
        record.Click = button.OnLeftClick.AsObservable().Subscribe(_ => OnButtonClicked());
        record.Hint = button.SetHint(m_ButtonTooltipText);
        return record;
    }

    private static void OnButtonClicked() {
        try {
            Feature.GetInstance<EnhancedMapToggleFeature>().ExecuteAction(default);
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: toggle button patch failed ({s_Failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling the toggle button after {MaxPatchFailures} failures. Recovery: UMM settings toggle or hotkey.");
            try {
                Feature.GetInstance<EnhancedMapToggleFeature>().Unpatch();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }
}
