using Kingmaker.Blueprints;

namespace ToyBox.Infrastructure.Utilities;

public static partial class ContextProvider {
    private static bool m_BlueprintProviderShown = false;
    public static bool Blueprint<T>(out T? bp) where T : SimpleBlueprint {
        using (VerticalScope()) {
            bp = BlueprintPicker<T>.CurrentBlueprint;
            string str;
            if (bp != null) {
                str = ": " + $"{BPHelper.GetTitle(bp)}".Green().Bold() + $" ({bp.AssetGuid})".Grey();
            } else {
                str = ": " + SharedStrings.NoneText.Red();
            }
            _ = UI.DisclosureToggle(ref m_BlueprintProviderShown, SharedStrings.CurrentlySelectedBlueprintText + str);
            if (m_BlueprintProviderShown) {
                m_BlueprintProviderShown = !BlueprintPicker<T>.OnPickerGUI();
            }
            bp = BlueprintPicker<T>.CurrentBlueprint;
        }
        return bp != null;
    }
}
