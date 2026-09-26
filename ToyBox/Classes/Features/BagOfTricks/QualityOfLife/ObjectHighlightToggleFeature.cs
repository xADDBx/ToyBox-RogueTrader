using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UI.InputSystems;
using Kingmaker.UI.Models.SettingsUI;
using Kingmaker.Utility.GameConst;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.QualityOfLife.ObjectHighlightToggleFeature")]
public partial class ObjectHighlightToggleFeature : FeatureWithPatch, IGameModeHandler {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableObjectHighlightToggle;
        }
    }
    private bool m_IsSubscribed = false;
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_ObjectHighlightToggleFeature_Name", "Object Highlight Toggle Mode")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_ObjectHighlightToggleFeature_Description", "Turns the object highlight key into a toggle (press to switch) outside of combat. Logs a note when Enhanced Controls is also installed (both drive the same highlight controller).")]
    public override partial string Description { get; }

    // EnhancedControls also drives the highlight controller with private
    // state our patches cannot see: running both can leave the highlight
    // stuck on or desync EC's state. We deliberately do NOT auto-disable -
    // the player decides - we just note the combination once per session.
    private static bool s_ConflictNoted;
    private static Type? s_EnhancedControlsType;
    private static int s_ScannedAssemblyCount;

    private static Type? FindEnhancedControls() {
        // Latch only the POSITIVE verdict: ToyBox loads before
        // EnhancedControls, so a not-found at mod load must not stick for the
        // whole session (assemblies are never unloaded once found).
        if (s_EnhancedControlsType != null) {
            return s_EnhancedControlsType;
        }
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        if (assemblies.Length == s_ScannedAssemblyCount) {
            // Same assembly count means the same set (assemblies never
            // unload) - skip the rescan on every re-check.
            return null;
        }
        s_ScannedAssemblyCount = assemblies.Length;
        foreach (var assembly in assemblies) {
            s_EnhancedControlsType = assembly.GetType("EnhancedControls.Main", throwOnError: false);
            if (s_EnhancedControlsType != null) {
                break;
            }
        }
        return s_EnhancedControlsType;
    }

    // ToyBox loads before EnhancedControls, so Enable() at mod load cannot
    // see it yet - OnGameModeStart re-checks (game mode events only fire
    // once every mod is loaded) and notes the combination.
    private static void NoteEnhancedControlsConflict() {
        if (s_ConflictNoted || FindEnhancedControls() == null) {
            return;
        }
        s_ConflictNoted = true;
        Warn("ToyBox: Object Highlight Toggle and Enhanced Controls both drive the object highlight; if highlighting ever sticks, disable one of them.");
    }

    public override void Enable() {
        NoteEnhancedControlsConflict();
        base.Enable();
        if (IsEnabled && !m_IsSubscribed) {
            _ = EventBus.Subscribe(this);
            m_IsSubscribed = true;
        }
    }
    public override void Disable() {
        base.Disable();
        if (m_IsSubscribed) {
            EventBus.Unsubscribe(this);
            m_IsSubscribed = false;
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.QualityOfLife.ObjectHighlightToggleFeature";
        }
    }
    private static readonly HashSet<GameModeType> m_TurnOffWhen = [GameModeType.Dialog, GameModeType.Cutscene, GameModeType.CutsceneGlobalMap];
    private static bool m_WasTurnedOffBefore = false;
    private static bool m_WasTurnedOff = false;
    private static bool m_JustChangedViaBinding = false;
    public void OnGameModeStart(GameModeType gameMode) {
        NoteEnhancedControlsConflict();
        if (Game.Instance.Player.IsInCombat) {
            return;
        }
        if (m_TurnOffWhen.Contains(gameMode)) {
            if (InteractionHighlightController.Instance?.IsHighlighting ?? false) {
                m_WasTurnedOffBefore = true;
                m_WasTurnedOff = true;
                InteractionHighlightController.Instance?.HighlightOff();
                m_WasTurnedOff = false;
            }
        } else {
            if (m_WasTurnedOffBefore && (!InteractionHighlightController.Instance?.IsHighlighting ?? false)) {
                InteractionHighlightController.Instance?.HighlightOn();
                m_WasTurnedOffBefore = false;
            }
        }
    }
    public void OnGameModeStop(GameModeType gameMode) {
        return;
    }
    [HarmonyPatch(typeof(KeyboardAccess), nameof(KeyboardAccess.OnCallbackByBinding)), HarmonyPrefix]
    private static bool KeyboardAccess_OnCallbackByBinding_Patch(KeyboardAccess.Binding binding) {
        if (Game.Instance?.Player?.IsInCombat ?? false) {
            return true;
        }
        if (binding.Name.StartsWith(UISettingsRoot.Instance.UIKeybindGeneralSettings.HighlightObjects.name)) {
            if (!m_JustChangedViaBinding && binding.InputMatched() && binding.Name.EndsWith(UIConsts.SuffixOn)) {
                m_JustChangedViaBinding = true;
                try {
                    InteractionHighlightController.Instance?.Highlight(!InteractionHighlightController.Instance?.IsHighlighting ?? false);
                } catch {
                    m_JustChangedViaBinding = false;
                    return false;
                }
                _ = Task.Delay(250).ContinueWith(_ => {
                    m_JustChangedViaBinding = false;
                });
            }
            return false;
        }
        return true;
    }
    private static bool m_InterestingTick = false;
    private static bool m_WasOnBeforeFightIntern = false;
    internal static bool m_WasOnBeforeFight = false;
    [HarmonyPatch(typeof(Player), nameof(Player.IsInCombat), MethodType.Setter), HarmonyPrefix]
    private static void Set_Player_IsInCombatPre(bool value) {
        m_InterestingTick = value != Game.Instance.Player.IsInCombat;
        if (!m_InterestingTick) {
            return;
        }

        if ((InteractionHighlightController.Instance?.IsHighlighting ?? false) && value) {
            m_WasOnBeforeFightIntern = true;
            m_WasOnBeforeFight = true;
            try {
                InteractionHighlightController.Instance.HighlightOff();
            } catch { }
            m_WasOnBeforeFight = false;
        }
    }
    [HarmonyPatch(typeof(Player), nameof(Player.IsInCombat), MethodType.Setter), HarmonyPostfix]
    private static void Set_Player_IsInCombatPost(bool value) {
        if (!m_InterestingTick) {
            return;
        }

        if (m_WasOnBeforeFightIntern && !value) {
            m_WasOnBeforeFightIntern = false;
            try {
                InteractionHighlightController.Instance?.HighlightOn();
            } catch { }
        }
        m_InterestingTick = false;
    }
    [HarmonyPatch(typeof(InteractionHighlightController), nameof(InteractionHighlightController.HighlightOff)), HarmonyPrefix]
    private static bool InteractionHighlightController_HighlightOff_Patch() {
        if (Game.Instance.Player.IsInCombat) {
            return true;
        }

        if (!m_WasOnBeforeFight && !m_WasTurnedOff && !m_JustChangedViaBinding) {
            return false;
        }
        return true;
    }
}
