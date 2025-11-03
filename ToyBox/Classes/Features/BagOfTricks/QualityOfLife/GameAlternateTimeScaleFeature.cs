using Kingmaker;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;
public partial class GameAlternateTimeScaleFeature : ToggledFeature, IBindableFeature {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableGameAlternateTimeScale;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_GameAlternateTimeScaleFeature_Name", "Alternate Game Time Scale")]
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
        if (IsEnabled) {
            Game.Instance.TimeController.DebugTimeScale = Settings.GameAlternateTimeScaleMultiplier;
        }
    }
    public override void Destroy() {
        base.Destroy();
        Game.Instance.TimeController.DebugTimeScale = GetInstance<GameTimeScaleFeature>().Value;
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Slider(ref Settings.GameAlternateTimeScaleMultiplier, 0.00001f, 20f, 1f, 3, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150))) {
                if (IsEnabled) {
                    Game.Instance.TimeController.DebugTimeScale = Settings.GameAlternateTimeScaleMultiplier;
                }
            }
            Space(10);
            base.OnGui();
            Space(10);
            var current = Keybind;
            if (UI.HotkeyPicker(ref current, this)) {
                Keybind = current;
            }
        }
    }
    public void ExecuteAction(params object[] parameter) {
        LogExecution();
        IsEnabled = !IsEnabled;
        if (IsEnabled) {
            Initialize();
        } else {
            Destroy();
        }
    }

    public void LogExecution(params object[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
}
