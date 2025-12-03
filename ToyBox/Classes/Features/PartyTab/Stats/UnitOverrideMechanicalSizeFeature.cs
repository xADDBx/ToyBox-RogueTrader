using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.View;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.PartyTab.Stats.UnitOverrideMechanicalSizeFeature")]
public partial class UnitOverrideMechanicalSizeFeature : FeatureWithPatch, INeedContextFeature<BaseUnitEntity> {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableUnitOverrideMechanicalSize;
        }
    }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideMechanicalSizeFeature_Name", "Enable Mechanical Size Overrides")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideMechanicalSizeFeature_Description", "If this is disabled, none of the previously set mechanical size overrides will take effect.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.PartyTab.Stats.UnitOverrideMechanicalSizeFeature";
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
    private Size? m_CurrentlySelected;
    private string MaybeGetLocalizedSize(Size size) {
        var localized = LocalizedTexts.Instance.Sizes.GetText(size);
        if (string.IsNullOrWhiteSpace(localized)) {
            return size.ToString();
        } else {
            return localized;
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        base.OnGui();
        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(20);
                using (VerticalScope()) {
                    UI.Label(m_CurrentMechanicalSizeLocalizedText + ": " + MaybeGetLocalizedSize(unit.State.Size));
                    if (InSaveSettings?.MechanicalSizeOverrides.TryGetValue(unit.UniqueId, out var current) ?? false) {
                        m_CurrentlySelected = current;
                    } else {
                        m_CurrentlySelected = null;
                    }
                    using (HorizontalScope()) {
                        UI.Label(m_OverrideLocalizedText + ": ");
                        if (UI.SelectionGrid(ref m_CurrentlySelected, 6, size => size.HasValue ? MaybeGetLocalizedSize(size.Value) : SharedStrings.NoneText)) {
                            if (m_CurrentlySelected.HasValue) {
                                InSaveSettings?.MechanicalSizeOverrides[unit.UniqueId] = m_CurrentlySelected.Value;
                                unit.State.Size = m_CurrentlySelected.Value;
                            } else {
                                InSaveSettings?.MechanicalSizeOverrides.Remove(unit.UniqueId);
                                unit.State.Size = unit.OriginalSize;
                            }
                            InSaveSettings?.Save();
                            unit.ViewTransform.localScale = unit.View.m_OriginalScale * (unit.View.m_Scale = unit.View.GetSizeScale());
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(UnitEntityView), nameof(UnitEntityView.GetSizeScale)), HarmonyPrefix]
    private static void UnitEntityView_GetSizeScale_Patch(UnitEntityView __instance) {
        if (__instance.EntityData != null && (InSaveSettings?.MechanicalSizeOverrides.TryGetValue(__instance.EntityData.UniqueId, out var size) ?? false)) {
            __instance.EntityData.State.m_Size = size;
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideMechanicalSizeFeature_m_CurrentMechanicalSizeLocalizedText", "Current Mechanical Size")]
    private static partial string m_CurrentMechanicalSizeLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitOverrideMechanicalSizeFeature_m_OverrideLocalizedText", "Override")]
    private static partial string m_OverrideLocalizedText { get; }
}
