using UnityEngine;

namespace ToyBox;

public abstract class FeatureWithIntSlider : Feature {
    public override void Enable() {
        base.Enable();
        IsInitialized = true;
    }
    public override void Disable() {
        base.Disable();
        IsInitialized = false;
    }
    protected bool IsInitialized = false;
    public abstract bool IsEnabled { get; }
    public abstract ref int Value { get; }
    public abstract int Min { get; }
    public abstract int Max { get; }
    public abstract int? Default { get; }
    protected virtual void OnValueChanged((int oldValue, int newValue) vals) {
        if (IsEnabled) {
            if (!IsInitialized) {
                Enable();
            }
        } else {
            if (IsInitialized) {
                Disable();
            }
        }
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            _ = UI.Slider(ref Value, Min, Max, Default, OnValueChanged, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
}
