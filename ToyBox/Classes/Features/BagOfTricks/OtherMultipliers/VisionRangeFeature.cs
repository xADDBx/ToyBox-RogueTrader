using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Owlcat.Runtime.Visual.FogOfWar;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.OtherMultipliers;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.OtherMultipliers.VisionRangeFeature")]
public partial class VisionRangeFeature : FeatureWithPatch {
    private bool m_IsEnabled;
    public override ref bool IsEnabled {
        get {
            m_IsEnabled = Settings.VisionRangeMultiplier.HasValue;
            return ref m_IsEnabled;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_VisionRangeFeature_Name", "Vision Range Multiplier")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_OtherMultipliers_VisionRangeFeature_Description", "Multiplies the fog of war reveal range of party members, pets, and player ships.")]
    public override partial string Description { get; }
    public override void OnGui() {
        var value = Settings.VisionRangeMultiplier ?? 1f;
        using (HorizontalScope()) {
            if (UI.LogSlider(ref value, 0f, 20f, 1f, 2, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150))) {
                if (value == 1f) {
                    Settings.VisionRangeMultiplier = null;
                    Disable();
                } else {
                    Settings.VisionRangeMultiplier = value;
                    Enable();
                }
            }
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.OtherMultipliers.VisionRangeFeature";
        }
    }
    private static bool IsPlayerRevealer(FogOfWarRevealerSettings revealer) {
        if (revealer.m_EntityView?.Data is not BaseUnitEntity unit) {
            return false;
        }
        var player = Game.Instance?.Player;
        return player != null && unit.Faction.IsPlayer && (player.PartyAndPets.Contains(unit) || player.AllStarships.Contains(unit));
    }
    [HarmonyPatch(typeof(FogOfWarScheduleController), nameof(FogOfWarScheduleController.CalculateFullRevealerRadius)), HarmonyPostfix]
    private static void FogOfWarScheduleController_CalculateFullRevealerRadius_Patch(FogOfWarRevealerSettings revealer, ref float __result) {
        if (IsPlayerRevealer(revealer) && Settings.VisionRangeMultiplier is { } multiplier) {
            __result *= multiplier;
        }
    }
    [HarmonyPatch(typeof(FogOfWarRevealerSettings), nameof(FogOfWarRevealerSettings.GetRevealerRange)), HarmonyPostfix]
    private static void FogOfWarRevealerSettings_GetRevealerRange_Patch(FogOfWarRevealerSettings __instance, FogOfWarSettings fow, ref float __result) {
        if (!IsPlayerRevealer(__instance) || Settings.VisionRangeMultiplier is not { } multiplier) {
            return;
        }
        if (__instance.MaskTexture != null) {
            __result *= multiplier;
        } else {
            var padding = fow.BorderOffset + (__instance.DefaultRadius ? 0f : fow.BorderWidth);
            __result = ((__result - padding) * multiplier) + padding;
        }
    }
    [HarmonyPatch(typeof(FogOfWarRevealerSettings), "UpdateRevealer"), HarmonyPostfix]
    private static void FogOfWarRevealerSettings_UpdateRevealer_Patch(FogOfWarRevealerSettings __instance) {
        if (__instance.MaskTexture != null && IsPlayerRevealer(__instance) && Settings.VisionRangeMultiplier is { } multiplier) {
            __instance.Revealer.Scale *= multiplier;
        }
    }
}
