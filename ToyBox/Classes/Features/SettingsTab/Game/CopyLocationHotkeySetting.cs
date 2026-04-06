using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using ToyBox.Classes.Infrastructure.Features;
using UnityEngine;

namespace ToyBox.Features.SettingsTab.Game;

public partial class CopyLocationHotkeySetting : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_SettingsTab_Game_CopyLocationHotkeySetting_Name", "Hotkey to copy location to clipboard")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Game_CopyLocationHotkeySetting_Name", "Pressing this hotkey causes the location under the mouse to be copied to the clipboard (for modders).")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame() && Kingmaker.Game.Instance.CurrentlyLoadedArea != null && Camera.main != null;
    }
    private static bool TryGetWorldPositionUnderMouse(Camera cam, out Vector3 worldPosition) {
        worldPosition = Vector3.zero;
        var mousePos = Input.mousePosition;
        var ray = cam.ScreenPointToRay(mousePos);

        var layerMask = ~0; // all layers

        const float maxDistance = 3000f;
        if (Physics.Raycast(ray, out var hit, maxDistance, layerMask)) {
            worldPosition = hit.point;
            return true;
        }

        // Fallback: raycast against a horizontal plane at y = 0.
        var groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out var enter)) {
            worldPosition = ray.GetPoint(enter);
            return true;
        }

        return false;
    }

    public override void ExecuteAction(ActionParameter parameter) {
        if (TryGetWorldPositionUnderMouse(Camera.main, out var position)) {
            CopyToClipboard($"Current Area: {Kingmaker.Game.Instance.CurrentlyLoadedArea.AssetGuid}; Location: ({position.x}f, {position.y}f, {position.z}f)");
        } else {
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning(m_ToyBox_FailedToCopyLocation_NoHLocalizedText, false));
        }
    }
    private static void CopyToClipboard(string s) {
        GUIUtility.systemCopyBuffer = s;
        EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning(m_ToyBox_CopiedLocationToClipboarLocalizedText + " " + s, false));
    }

    [LocalizedString("ToyBox_Features_SettingsTab_Game_CopyLocationHotkeySetting_m___ToyBox_FailedToCopyLocation_NoHLocalizedText", "[ToyBox] Failed to copy location: No hit for raycast.")]
    private static partial string m_ToyBox_FailedToCopyLocation_NoHLocalizedText { get; }
    [LocalizedString("ToyBox_Features_SettingsTab_Game_CopyLocationHotkeySetting_m___ToyBox_CopiedLocationToClipboarLocalizedText", "[ToyBox] Copied Location to clipboard:")]
    private static partial string m_ToyBox_CopiedLocationToClipboarLocalizedText { get; }
}
