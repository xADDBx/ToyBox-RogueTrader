using UnityEngine;

namespace ToyBox;

public abstract class FeatureWithFloatSlider : Feature {
    public override void Initialize() {
        base.Initialize();
        IsInitialized = true;
    }
    public override void Destroy() {
        base.Destroy();
        IsInitialized = false;
    }
    protected bool IsInitialized = false;
    public abstract bool IsEnabled { get; }
    public abstract ref float Value { get; }
    public abstract float Min { get; }
    public abstract float Max { get; }
    public virtual int Digits { get; } = 2;
    public abstract float? Default { get; }
    protected virtual void OnValueChanged((float oldValue, float newValue) vals) {
        if (IsEnabled) {
            if (!IsInitialized) {
                Initialize();
            }
        } else {
            if (IsInitialized) {
                Destroy();
            }
        }
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            _ = UI.Slider(ref Value, Min, Max, Default, Digits, OnValueChanged, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
}
