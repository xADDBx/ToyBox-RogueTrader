using Kingmaker;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;
using static AddRandomToUV1;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;
public partial class GameAlternateTimeScaleFeature : ToggledFeature, IBindableFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableGameAlternateTimeScale;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_GameAlternateTimeScaleFeature_Name", "Game Time Scale")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_GameAlternateTimeScaleFeature_Description", "Optional alternate time scale which can be swapped to via hotkey.")]
    public override partial string Description { get; }
    public Hotkey? Keybind {
        get;
        private set;
    }
    public override void Initialize() {
        base.Initialize();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
        Game.Instance.TimeController.DebugTimeScale = Settings.GameAlternateTimeScaleMultiplier;
    }
    public override void Destroy() {
        base.Destroy();
        Game.Instance.TimeController.DebugTimeScale = GetInstance<GameTimeScaleFeature>().Value;
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            base.OnGui();
            Space(10);
            var current = Keybind;
            if (UI.HotkeyPicker(ref current, this, true)) {
                Keybind = current;
            }
        }
        if (UI.Slider(ref Settings.GameAlternateTimeScaleMultiplier, 0.00001f, 20f, 1f, 3, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150))) {
            if (IsEnabled) {
                Game.Instance.TimeController.DebugTimeScale = Settings.GameAlternateTimeScaleMultiplier;
            }
        }
    }
    public void ExecuteAction(params object[] parameter) {
        LogExecution();
        IsEnabled = !IsEnabled;
    }

    public void LogExecution(params object[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
}
