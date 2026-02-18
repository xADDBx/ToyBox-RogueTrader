namespace ToyBox;

public abstract class FeatureWIthUnitSelectTypeGrid : FeatureWithPatch {
    private bool m_IsEnabled;
    public override ref bool IsEnabled {
        get {
            return ref m_IsEnabled;
        }
    }

    public abstract ref UnitSelectType SelectSetting { get; }
    public override void Enable() {
        UpdateEnabled();
        base.Enable();
    }
    public override void Disable() {
        UpdateEnabled();
        base.Disable();
    }
    private void UpdateEnabled() {
        m_IsEnabled = SelectSetting != UnitSelectType.Off;
    }
    public override void OnGui() {
        using (VerticalScope()) {
            using (HorizontalScope()) {
                Space(27);
                UI.Label(Name);
                Space(10);
                UI.Label(Description.Green());
            }
            using (HorizontalScope()) {
                Space(150);
                if (UI.SelectionGrid(ref SelectSetting, 6, (type) => type.GetLocalized(), AutoWidth())) {
                    if (SelectSetting != UnitSelectType.Off) {
                        if (!m_IsEnabled) {
                            Enable();
                        }
                    } else {
                        if (m_IsEnabled) {
                            Disable();
                        }
                    }
                }
            }
        }
    }
}
