using Kingmaker.View;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Camera;

public partial class ResetCameraAimToDefaultFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_ResetCameraAimToDefaultFeature_Name", "Fix Camera")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_ResetCameraAimToDefaultFeature_Description", "Resets the camera perspective to what it was before orbiting it.")]
    public override partial string Description { get; }
    public override void Enable() {
        base.Enable();
    }
    public override bool CanExecute(ActionParameter parameter) {
        return true;
    }
    public override void ExecuteAction(ActionParameter parameter) {
        LogExecution(parameter);
        var rig = CameraRig.Instance;
        _ = rig?.m_TargetRotate.x = 0;
    }
}
