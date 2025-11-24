using Kingmaker.View;
using ToyBox.Infrastructure.Keybinds;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.BagOfTricks.Camera;

[HarmonyPatch, ToyBoxPatchCategory("ToyBox.Features.BagOfTricks.Camera.AllowMouse3DraggingToAimCameraFeature")]
public partial class AllowMouse3DraggingToAimCameraFeature : FeatureWithPatch, IBindableFeature {
    private static float m_OriginalMinSpaceCameraAngle;
    private static float m_OriginalMaxSpaceCameraAngle;
    private static bool m_OriginalEnableOrbitCamera;
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableMouse3DraggingToAimCamera;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_AllowMouse3DraggingToAimCameraFeature_Name", "Enable Mouse3 Dragging To Aim The Camera")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Camera_AllowMouse3DraggingToAimCameraFeature_Description", "Allows orbiting the camera while holding down Mouse Wheel.")]
    public override partial string Description { get; }

    protected override string HarmonyName {
        get {
            return "ToyBox.Features.BagOfTricks.Camera.AllowMouse3DraggingToAimCameraFeature";
        }
    }
    public override void Initialize() {
        base.Initialize();
        Keybind = Hotkeys.MaybeGetHotkey(GetType());
    }
    public Hotkey? Keybind {
        get;
        private set;
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
    public void LogExecution(params object?[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
    public override void OnGui() {
        using (HorizontalScope()) {
            base.OnGui();
            Space(10);
            var current = Keybind;
            if (UI.HotkeyPicker(ref current, this)) {
                Keybind = current;
            }
        }
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate)), HarmonyPrefix]
    private static void CameraRige_TickRotate_PrePatch(CameraRig __instance) {
        m_OriginalMinSpaceCameraAngle = __instance.MinSpaceCameraAngle;
        m_OriginalMaxSpaceCameraAngle = __instance.MaxSpaceCameraAngle;
        m_OriginalEnableOrbitCamera = __instance.m_EnableOrbitCamera;
        __instance.MinSpaceCameraAngle = -63;
        __instance.MaxSpaceCameraAngle = 100;
        __instance.m_EnableOrbitCamera = true;
    }
    [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate)), HarmonyPostfix]
    private static void CameraRige_TickRotate_PostPatch(CameraRig __instance) {
        __instance.MinSpaceCameraAngle = m_OriginalMinSpaceCameraAngle;
        __instance.MaxSpaceCameraAngle = m_OriginalMaxSpaceCameraAngle;
        __instance.m_EnableOrbitCamera = m_OriginalEnableOrbitCamera;
    }
}
