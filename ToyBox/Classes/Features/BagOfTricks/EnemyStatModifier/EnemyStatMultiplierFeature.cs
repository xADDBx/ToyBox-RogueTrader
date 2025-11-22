using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Stats.Base;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;
using Warhammer.SpaceCombat.StarshipLogic;

namespace ToyBox.Features.BagOfTricks.EnemyStatModifier;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.EnemyStatModifier.EnemyStatMultiplierFeature")]
public partial class EnemyStatMultiplierFeature : FeatureWithPatch {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableEnemyStatMultiplier;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_EnemyStatModifier_EnemyStatMultiplierFeature_Name", "Enemy Stat Multiplier")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_EnemyStatModifier_EnemyStatMultiplierFeature_Description", "Allows adding stat multipliers, e.g. Enemy Health x3.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.EnemyStatModifier.EnemyStatMultiplierFeature";
        }
    }

    private readonly TimedCache<float> m_LabelWidth = new(() => {
        List<string> names = [];
        foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
            if (Constants.NonHumanStats.Contains(stat) || Constants.LegacyStats.Contains(stat)) {
                continue;
            }
            var name = LocalizedTexts.Instance.Stats.GetText(stat);
            if (string.IsNullOrWhiteSpace(name)) {
                name = stat.ToString();
            }
            names.Add(name);
        }
        return CalculateLargestLabelSize(names, GUI.skin.label);
    });
    public override void OnGui() {
        base.OnGui();

        if (IsEnabled) {
            using (HorizontalScope()) {
                Space(25);
                using (VerticalScope()) {
                    foreach (StatType stat in Enum.GetValues(typeof(StatType))) {
                        if (Constants.NonHumanStats.Contains(stat) || Constants.LegacyStats.Contains(stat)) {
                            continue;
                        }
                        _ = Settings.MultiplierEnemyMods.TryGetValue(stat, out var mod);
                        using (HorizontalScope()) {
                            var name = LocalizedTexts.Instance.Stats.GetText(stat);
                            if (string.IsNullOrWhiteSpace(name)) {
                                name = stat.ToString();
                            }
                            UI.Label(name, Width(m_LabelWidth + 50 * Main.UIScale));
                            if (UI.Slider(ref mod, -100, 100, 1, 2)) {
                                if (mod == 1) {
                                    Settings.MultiplierEnemyMods.Remove(stat);
                                } else {
                                    Settings.MultiplierEnemyMods[stat] = mod;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(ModifiableValue), nameof(ModifiableValue.ModifiedValue), MethodType.Getter), HarmonyPostfix]
    private static void ModifiableValue_getModifiedValue_Patch(ModifiableValue __instance, ref int __result) {
        if (Constants.NonHumanStats.Contains(__instance.OriginalType) || Constants.LegacyStats.Contains(__instance.OriginalType)) {
            return;
        }
        if (__instance.Owner is BaseUnitEntity entity && !entity.IsStarship() && entity.IsPlayerEnemy && Settings.MultiplierEnemyMods.TryGetValue(__instance.OriginalType, out var mod)) {
            __result = (int)(mod * __result);
        }
    }
}
