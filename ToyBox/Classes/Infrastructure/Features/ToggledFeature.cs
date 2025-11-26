namespace ToyBox;

public abstract class ToggledFeature : Feature {
    public abstract ref bool IsEnabled { get; }
    public override void OnGui() {
        if (this is IToggledWithBinding bindable) {
            using (HorizontalScope()) {
                _ = UI.Toggle(Name, Description, ref IsEnabled, Initialize, Destroy);
                Space(10);
                var current = bindable.Keybind;
                if (UI.HotkeyPicker(ref current, bindable)) {
                    bindable.Keybind = current;
                }
            }
        } else {
            _ = UI.Toggle(Name, Description, ref IsEnabled, Initialize, Destroy);
        }
    }
}
