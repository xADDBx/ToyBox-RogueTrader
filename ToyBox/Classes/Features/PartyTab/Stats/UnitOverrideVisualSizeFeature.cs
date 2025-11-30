using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PartyTab.Stats.UnitOverrideVisualSizeFeature")]
public partial class UnitOverrideVisualSizeFeature : FeatureWithPatch, INeedContextFeature<BaseUnitEntity> {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableUnitOverrideVisualSize;
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideVisualSizeFeature_Name", "Enable Visual Size Overrides")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideVisualSizeFeature_Description", "If this is disabled, none of the previously set visual size overrides will take effect.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PartyTab.Stats.UnitOverrideVisualSizeFeature";
        }
    }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        base.OnGui();
        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(20);
                UI.Label(m_VisualOverrideLocalizedText + ": ");
                var current = 1f;
                InSaveSettings?.VisualSizeOverrides.TryGetValue(unit.UniqueId, out current);
                Space(10);
                if (UI.LogSlider(ref current, 0.01f, 40f, 1, 2, null, Width(300 * Main.UIScale))) {
                    if (current == 1) {
                        InSaveSettings?.VisualSizeOverrides.Remove(unit.UniqueId);
                    } else {
                        InSaveSettings?.VisualSizeOverrides[unit.UniqueId] = current;
                    }
                    InSaveSettings?.Save();
                    unit.ViewTransform.localScale = unit.View.m_OriginalScale * (unit.View.m_Scale = unit.View.GetSizeScale());
                }
            }
        }
    }
    [HarmonyPatch(typeof(UnitEntityView), nameof(UnitEntityView.GetSizeScale)), HarmonyPostfix]
    private static void UnitEntityView_GetSizeScale_Patch(ref float __result, UnitEntityView __instance) {
        if (__instance.EntityData != null && (InSaveSettings?.VisualSizeOverrides.TryGetValue(__instance.EntityData.UniqueId, out var scale) ?? false)) {
            __result *= scale;
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideVisualSizeFeature_m_VisualOverrideLocalizedText", "Visual Override")]
    private static partial string m_VisualOverrideLocalizedText { get; }
}
