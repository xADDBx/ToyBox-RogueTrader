using System.Reflection;
using Kingmaker.Blueprints.Items.Augments;
using Kingmaker.Items;
using Kingmaker.Items.Slots;

namespace ToyBox.Features.BagOfTricks.Common;

// Game bug: UnitAugments.OnPostLoad() foreach-enumerates m_Slots while
// RemoveSpecialSlotIfReleased() removes entries from that same dictionary,
// throwing InvalidOperationException and leaving the unit half-initialized
// (downstream symptom: UnitLineOfSightCacheController NRE every frame).
// Triggers when any special augment slot lost its SpecialUnlock retain
// (e.g. mod-recovered placeholder items on load). We pre-remove those slots
// over a snapshot (same steps the game takes), so the game's own loop only
// ever calls InitializeOnPostLoad and never mutates m_Slots mid-iteration.
// House rule: the original method is never skipped — the prefix is void.
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Common.AugmentPostLoadFixFeature")]
public partial class AugmentPostLoadFixFeature : FeatureWithPatch {
    private static readonly FieldInfo SlotsField = AccessTools.Field(typeof(UnitAugments), "m_Slots");

    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableAugmentPostLoadFix;
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Common.AugmentPostLoadFixFeature";
        }
    }

    [HarmonyPatch(typeof(UnitAugments), nameof(UnitAugments.OnPostLoad)), HarmonyPrefix]
    private static void UnitAugments_OnPostLoad_Prefix(UnitAugments __instance) {
        try {
            if (__instance.Owner == null || SlotsField == null) {
                return;
            }
            var slots = (Dictionary<BlueprintAugmentSlot, AugmentSlot>)SlotsField.GetValue(__instance);
            if (slots == null) {
                return;
            }
            // Pre-remove every released special slot before the game's own loop
            // runs, using the same steps as RemoveSpecialSlotIfReleased. A removal
            // can raise item-removal events that release further slots, so repeat
            // until a full pass removes nothing; afterwards no remaining slot
            // matches the game's removal condition, its loop only calls
            // InitializeOnPostLoad, and m_Slots is never mutated mid-enumeration.
            var removedAny = true;
            while (removedAny) {
                removedAny = false;
                foreach (var slot in slots.ToArray()) {
                    if (!slot.Key.IsCommon() && !slot.Value.SpecialUnlock.Value) {
                        // inlined RemoveSpecialSlotIfReleased (private in game code)
                        if (slot.Value.HasItem) {
                            _ = slot.Value.RemoveItem(autoMerge: true, force: true);
                        }
                        _ = __instance.Owner.Body.AllSlots.Remove(slot.Value);
                        _ = slots.Remove(slot.Key);
                        removedAny = true;
                    }
                }
            }
        } catch (Exception ex) {
            Error(ex);
        }
    }

    [LocalizedString("ToyBox_Features_BagOfTricks_Common_AugmentPostLoadFixFeature_Name", "Fix Augment PostLoad Crash")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_AugmentPostLoadFixFeature_Description", "Fixes a game bug where UnitAugments.OnPostLoad removes entries while iterating them (InvalidOperationException 'Collection was modified'), which leaves augment-bearing units half-loaded and spams NullReferenceException every frame.")]
    public override partial string Description { get; }
}
