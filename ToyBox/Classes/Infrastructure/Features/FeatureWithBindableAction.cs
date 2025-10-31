using ToyBox.Infrastructure.Keybinds;
using UnityEngine;

namespace ToyBox;
[NeedsTesting]
public abstract partial class FeatureWithBindableAction : FeatureWithAction, IBindableFeature {
    public Hotkey? Keybind {
        get;
        private set;
    }
    public override void Initialize() {
        base.Initialize();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            if (UI.Button(Name)) {
                ExecuteAction();
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
