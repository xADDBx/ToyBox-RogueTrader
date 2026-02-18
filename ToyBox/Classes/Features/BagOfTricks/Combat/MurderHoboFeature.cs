using Kingmaker.Cheats;
using Kingmaker.Controllers.Combat;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.BagOfTricks.Combat;

[IsTested]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Combat.MurderHoboFeature")]
public partial class MurderHoboFeature : FeatureWithPatch, IToggledWithBinding {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_MurderHoboFeature_Name", "Murder Hobo")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_MurderHoboFeature_Description", "When enabled, enemies will be killed when they join combat.")]
    public override partial string Description { get; }
    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Combat.MurderHoboFeature";
        }
    }
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableMurderHobo;
        }
    }
    public override void Enable() {
        base.Enable();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public Hotkey? Keybind {
        get;
        set;
    }
    public void ExecuteAction(params object[] parameter) {
        LogExecution();
        IsEnabled = !IsEnabled;
        if (IsEnabled) {
            Enable();
        } else {
            Disable();
        }
    }
    public void LogExecution(params object?[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
    [HarmonyPatch(typeof(PartUnitCombatState), nameof(PartUnitCombatState.JoinCombat)), HarmonyPostfix]
    private static void MaybeKillEnemy(PartUnitCombatState __instance) {
        var unit = __instance.Owner;
        if (unit?.IsPlayerEnemy ?? false) {
            CheatsCombat.KillUnit(unit);
        }
    }
}
