using Kingmaker.Cheats;
using Kingmaker.Controllers.Combat;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.BagOfTricks.Combat;
[NeedsTesting]
[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Combat.MurderHoboFeature")]
public partial class MurderHoboFeature : FeatureWithPatch, IBindableFeature {
    [LocalizedString("ToyBox_Features_BagOfTricks_Combat_MurderHoboFeature_Name", "Murer Hobo")]
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
    public override void Initialize() {
        base.Initialize();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public Hotkey? Keybind {
        get;
        private set;
    }
    public void ExecuteAction(params object[] parameter) {
        LogExecution();
        IsEnabled = !IsEnabled;
    }
    public void LogExecution(params object?[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            base.OnGui();
            Space(10);
            var current = Keybind;
            if (UI.HotkeyPicker(ref current, this)) {
                Keybind = current;
            }
        }
    }
    [HarmonyPatch(typeof(PartUnitCombatState), nameof(PartUnitCombatState.JoinCombat)), HarmonyPostfix]
    private static void MaybeKillEnemy(PartUnitCombatState __instance) {
        var unit = __instance.Owner;
        if (unit?.IsPlayerEnemy ?? false) {
            CheatsCombat.KillUnit(unit);
        }
    }
}
