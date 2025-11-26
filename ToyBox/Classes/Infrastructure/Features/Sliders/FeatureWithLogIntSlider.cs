using UnityEngine;

namespace ToyBox;

public abstract class FeatureWithLogIntSlider : Feature {
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
    public abstract ref int Value { get; }
    public abstract int Min { get; }
    public abstract int Max { get; }
    public abstract int? Default { get; }
    protected virtual void OnValueChanged((int oldValue, int newValue) vals) {
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
            _ = UI.LogSlider(ref Value, Min, Max, Default, OnValueChanged, null, AutoWidth(), GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
            Space(10);
            UI.Label(Name);
            Space(10);
            UI.Label(Description.Green());
        }
    }
}
