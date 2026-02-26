using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Keybinds;

namespace ToyBox;

public abstract partial class FeatureWithBindableAction : FeatureWithAction, IBindableFeature {
    public Hotkey? Keybind {
        get;
        private set;
    }
    public override void Enable() {
        base.Enable();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public void LogExecution(ActionParameter parameter) {
        base.LogExecution(parameter);
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Button(Name)) {
                ExecuteAction(default);
            }
            Space(10);
            UI.Label(Description.Green());
            Space(10);
            var current = Keybind;
            if (UI.HotkeyPicker(ref current, this)) {
                Keybind = current;
            }
        }
    }
}
