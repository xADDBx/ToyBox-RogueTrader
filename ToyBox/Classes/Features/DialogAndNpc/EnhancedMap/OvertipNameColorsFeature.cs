using Kingmaker.Code.UI.MVVM.View.Overtips.Unit.UnitOvertipParts;
using Kingmaker.Code.UI.MVVM.VM.Common.UnitState;
using Kingmaker.EntitySystem.Entities;
using System.Runtime.CompilerServices;
using TMPro;
using UniRx;
using UnityEngine;

namespace ToyBox.Features.DialogAndNpc.EnhancedMap;

/// <summary>
/// TAB nameplate coloring by dialog state (R5) plus the WenKai font
/// sub-option (R8). Four postfixes on the nameplate part views; writes ONLY
/// .color/.font (never alpha/visibility/layer) and restores the stored
/// original for non-classified units, on feature-off, and for dead units.
/// Enhanced Controls never writes color and FontMod only swaps font assets,
/// so we are the sole color writer.
/// </summary>
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.DialogAndNpc.EnhancedMap.OvertipNameColorsFeature")]
public partial class OvertipNameColorsFeature : FeatureWithPatch {
    private const int MaxPatchFailures = 3;

    // Immediate re-classification on Tab edges is bounded by TIME, not count:
    // per-unit condition trees vary ~0.05-0.3 ms, so a fixed count could still
    // blow the 2 ms Tab-edge frame budget in dense hubs. 1.5 ms leaves room
    // for vanilla's own HighlightOn sweep; the rest defers via the scheduler.
    private static readonly long s_EdgeBudgetTicks = (long)(System.Diagnostics.Stopwatch.Frequency * 1.5 / 1000.0);
    private static readonly System.Diagnostics.Stopwatch s_EdgeWatch = new();
    private static int s_EdgeFrame = -1;

    // Same encoding as the map markers for cross-surface consistency.
    private static readonly Color NewColor = new(1f, 0.85f, 0.25f, 1f);
    private static readonly Color RepeatColor = new(0.4f, 0.62f, 1f, 1f);

    private static int s_Failures;
    private static bool s_ColorsEnabled;
    private static bool s_PumpWired;

    private sealed class TextRecord {
        public Color OriginalColor;
        public TMP_FontAsset? OriginalFont;
        public BaseUnitEntity? Unit;
        public UnitState? Owner;
        public IDisposable? EdgeSubscription;
    }

    private static readonly ConditionalWeakTable<TextMeshProUGUI, TextRecord> s_Texts = new();

    // net481 ConditionalWeakTable has no enumeration; the holder list drives
    // the restore-on-off sweep and dead-view pruning (subscriptions must be
    // disposed even when the text is gone).
    private sealed class TextHolder {
        public readonly WeakReference<TextMeshProUGUI> Weak;
        public readonly TextRecord Record;

        public TextHolder(TextMeshProUGUI text, TextRecord record) {
            Weak = new WeakReference<TextMeshProUGUI>(text);
            Record = record;
        }
    }

    private static readonly List<TextHolder> s_Written = new();

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.DialogAndNpc.EnhancedMap.OvertipNameColorsFeature";
        }
    }

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableOvertipNameColors;
        }
    }

    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_OvertipNameColorsFeature_Name", "TAB Name Colors by Dialog State")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_OvertipNameColorsFeature_Description", "Colors unit nameplates while Tab is held: yellow = new dialog available, blue = repeatable dialog. Everything else (including teammates) keeps the vanilla color.")]
    public override partial string Description { get; }
    [LocalizedString("ToyBox_Features_DialogAndNpc_EnhancedMap_OvertipNameColorsFeature_m_UseWenKaiText", "Use WenKai font for nameplates (requires FontMod)")]
    private static partial string m_UseWenKaiText { get; }

    public override void OnGui() {
        using (VerticalScope()) {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Enable, Disable);
            if (IsEnabled) {
                using (HorizontalScope()) {
                    Space(50);
                    _ = UI.Toggle(m_UseWenKaiText, "", ref Settings.OvertipNameUseWenKai, RefreshFonts, RefreshFonts);
                }
            }
        }
    }

    public override void Enable() {
        base.Enable();
        // The pump must run for this feature alone too: with the markers
        // feature off, deferred nameplate classifications would otherwise
        // never complete and the queue strong-refs units for the session.
        NpcDialogClassifierRuntime.EnsureSubscribed();
        NpcDialogClassifierRuntime.EnsurePumpStarted();
        if (!s_PumpWired) {
            s_PumpWired = true;
            // Deferred classifications land in the cache a few frames after
            // bind/edge; without this repaint hook the color only reaches
            // the screen on the NEXT Tab press or rebind.
            NpcDialogClassifierRuntime.Pumped += OnPumped;
        }
        // Re-enable also resets the 3-strike counter (same policy as the
        // marker features: only consecutive failures may auto-disable).
        s_Failures = 0;
        s_ColorsEnabled = true;
    }

    // User-off live sweep: restore every color AND font we ever wrote and
    // drop the edge subscriptions so re-enabling starts clean (verdict
    // mandate: no tint survives until rebind; no accumulation on pooled
    // views).
    public override void Disable() {
        s_ColorsEnabled = false;
        if (s_PumpWired) {
            s_PumpWired = false;
            NpcDialogClassifierRuntime.Pumped -= OnPumped;
        }
        RestoreAll();
        base.Disable();
    }

    private static void OnPumped(object? sender, EventArgs eventArgs) {
        if (!s_ColorsEnabled) {
            return;
        }
        try {
            // Cache hits only (O(1) read + color write); misses stay queued -
            // the next batch repaints them. Tens of texts per batch.
            for (var i = s_Written.Count - 1; i >= 0; i--) {
                if (!s_Written[i].Weak.TryGetTarget(out var text)) {
                    // Prune dead holders in the hot path (area transitions
                    // destroy overtip texts; the same pattern as
                    // StarMapPlanetColorsFeature) so the strong Unit
                    // references cannot accumulate for the session.
                    s_Written.RemoveAt(i);
                    continue;
                }
                if (text != null) {
                    RecolorFromCache(text, s_Written[i].Record);
                }
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    private static void RefreshFonts() {
        for (var i = s_Written.Count - 1; i >= 0; i--) {
            if (!s_Written[i].Weak.TryGetTarget(out var text) || text == null) {
                DisposeHolder(s_Written[i]);
                s_Written.RemoveAt(i);
                continue;
            }
            if (Settings.OvertipNameUseWenKai) {
                WenKaiFontProvider.ApplyFont(text);
            } else if (s_Written[i].Record.OriginalFont != null && text.font != s_Written[i].Record.OriginalFont) {
                // Sub-toggle OFF must restore live AND pooled-bound plates,
                // not just skip future applies (v10 audit P1: without this
                // branch WenKai stuck until the whole feature was disabled).
                text.font = s_Written[i].Record.OriginalFont;
            }
        }
    }

    private static void RestoreAll() {
        for (var i = s_Written.Count - 1; i >= 0; i--) {
            var holder = s_Written[i];
            holder.Record.EdgeSubscription?.Dispose();
            holder.Record.EdgeSubscription = null;
            holder.Record.Owner = null;
            if (holder.Weak.TryGetTarget(out var text) && text != null) {
                text.color = holder.Record.OriginalColor;
                if (holder.Record.OriginalFont != null) {
                    text.font = holder.Record.OriginalFont;
                }
                _ = s_Texts.Remove(text);
            }
            s_Written.RemoveAt(i);
        }
    }

    private static void DisposeHolder(TextHolder holder) {
        holder.Record.EdgeSubscription?.Dispose();
        holder.Record.EdgeSubscription = null;
        holder.Record.Owner = null;
    }

    private static void HandleText(UnitState? unitState, TextMeshProUGUI? text) {
        try {
            if (text == null || unitState == null) {
                return;
            }
            var record = s_Texts.GetValue(text, t => {
                var created = new TextRecord { OriginalColor = t.color, OriginalFont = t.font };
                s_Written.Add(new TextHolder(t, created));
                return created;
            });
            // Pooled view rebound to a different unit: refresh the captured
            // UnitState so the edge subscription does not keep the first
            // bind's closure (stale unit -> wrong color).
            if (record.Owner != unitState) {
                record.EdgeSubscription?.Dispose();
                record.EdgeSubscription = null;
                record.Owner = unitState;
                // This postfix runs right after vanilla wrote whatever color
                // the NEW unit should have; re-capture it as the restore
                // baseline - but never while OUR color is on screen (Tab
                // held through the rebind), or our write would become the
                // "original" and never restore.
                if (text.color != NewColor && text.color != RepeatColor) {
                    record.OriginalColor = text.color;
                }
            }
            record.Unit = unitState.Unit.MechanicEntity as BaseUnitEntity;
            RecolorFromCache(text, record);
            if (Settings.OvertipNameUseWenKai) {
                WenKaiFontProvider.ApplyFont(text);
            }
            if (record.EdgeSubscription == null) {
                // Property edges, NOT the physical key: Enhanced Controls
                // rebinds the highlight key; UnitState.ForceHotKeyPressed is
                // the game's own funnel for TAB-driven visibility.
                record.EdgeSubscription = unitState.ForceHotKeyPressed.Subscribe((Action<bool>)(pressed
                    => OnHighlightEdge(text, record, unitState, pressed)));
            }
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            HandleFailure(ex);
        }
    }

    // Bind path: serve from cache only; a miss defers and leaves the color
    // untouched (vanilla) until the pump completes.
    private static void RecolorFromCache(TextMeshProUGUI text, TextRecord record) {
        var unit = record.Unit;
        if (unit == null || unit.LifeState.IsDead) {
            text.color = record.OriginalColor;
            return;
        }
        var state = NpcDialogClassifierRuntime.GetFreshState(unit);
        if (state == null) {
            _ = NpcDialogClassifierRuntime.Scheduler.Enqueue(unit);
            return;
        }
        ApplyState(text, record, state.Value);
    }

    // Edge path: invalidate ONLY on the rising edge, then read through the
    // budgeted immediate path (the same evaluation map-open uses); units
    // beyond the time budget defer and keep vanilla. Tab release restores the
    // original color everywhere.
    private static void OnHighlightEdge(TextMeshProUGUI text, TextRecord record, UnitState unitState, bool pressed) {
        try {
            if (!s_ColorsEnabled || text == null) {
                return;
            }
            if (!pressed) {
                text.color = record.OriginalColor;
                return;
            }
            var unit = unitState.Unit.MechanicEntity as BaseUnitEntity;
            record.Unit = unit;
            if (unit == null || unit.LifeState.IsDead) {
                text.color = record.OriginalColor;
                return;
            }
            NpcDialogClassifierRuntime.Invalidate(unit);
            if (Time.frameCount != s_EdgeFrame) {
                s_EdgeFrame = Time.frameCount;
                s_EdgeWatch.Restart();
            }
            if (s_EdgeWatch.ElapsedTicks < s_EdgeBudgetTicks) {
                ApplyState(text, record, NpcDialogClassifierRuntime.ClassifyNow(unit));
            } else {
                _ = NpcDialogClassifierRuntime.Scheduler.Enqueue(unit);
            }
            if (s_Failures > 0) {
                s_Failures = 0;
            }
        } catch (Exception ex) {
            // A destroyed TMP or dead wrapper must not throw into the game's
            // reactive dispatch.
            HandleFailure(ex);
        }
    }

    private static void ApplyState(TextMeshProUGUI text, TextRecord record, NpcDialogMarkerState state) {
        // None/Unknown/Inactive and anything negative restores the original:
        // there is deliberately no "exhausted" state.
        text.color = state switch {
            NpcDialogMarkerState.New => NewColor,
            NpcDialogMarkerState.Repeat => RepeatColor,
            _ => record.OriginalColor,
        };
        if (state is NpcDialogMarkerState.New or NpcDialogMarkerState.Repeat
            && TmProbe.OncePerArea("nameplate-color")) {
            // TM-probe (v10): first classified color actually written this
            // area - proves the pump/cache/bind chain end to end.
            TmProbe.Log($"first nameplate color written state={state} color=#{ColorUtility.ToHtmlStringRGBA(text.color)}");
        }
    }

    private static void HandleFailure(Exception ex) {
        s_Failures++;
        if (s_Failures <= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: nameplate patch failed ({s_Failures}/{MaxPatchFailures}): {ex.Message}");
        }
        if (s_Failures >= MaxPatchFailures) {
            Warn($"ToyBox EnhancedMap: auto-disabling OvertipNameColorsFeature after {MaxPatchFailures} failures. Recovery: toggle the feature in ToyBox settings.");
            try {
                var feature = Feature.GetInstance<OvertipNameColorsFeature>();
                feature.IsEnabled = false;
                feature.Disable();
            } catch {
                // Feature tab may not be constructed during early load.
            }
        }
    }

    [HarmonyPatch(typeof(OvertipNameBlockPCView), "BindViewImplementation"), HarmonyPostfix]
    private static void OvertipNameBlockPCView_BindViewImplementation_Postfix(OvertipNameBlockPCView __instance) {
        HandleText(__instance.ViewModel?.UnitState, __instance.m_CharacterName);
    }

    [HarmonyPatch(typeof(OvertipNameView), "BindViewImplementation"), HarmonyPostfix]
    private static void OvertipNameView_BindViewImplementation_Postfix(OvertipNameView __instance) {
        HandleText(__instance.ViewModel?.UnitState, __instance.m_CharacterName);
    }

    [HarmonyPatch(typeof(OvertipUnitNameView), "BindViewImplementation"), HarmonyPostfix]
    private static void OvertipUnitNameView_BindViewImplementation_Postfix(OvertipUnitNameView __instance) {
        HandleText(__instance.ViewModel?.UnitState, __instance.m_NameText);
    }

    [HarmonyPatch(typeof(OvertipLightweightUnitNameView), "BindViewImplementation"), HarmonyPostfix]
    private static void OvertipLightweightUnitNameView_BindViewImplementation_Postfix(OvertipLightweightUnitNameView __instance) {
        HandleText(__instance.ViewModel?.UnitState, __instance.m_NameText);
    }
}
