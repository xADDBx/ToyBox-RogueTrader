namespace ToyBox;

public abstract class ToggledFeature : Feature {
    public abstract ref bool IsEnabled { get; }
    public override void OnGui() {
        _ = UI.Toggle(Name, Description, ref IsEnabled, Initialize, Destroy);
    }
}
