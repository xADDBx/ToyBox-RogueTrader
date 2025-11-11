using Kingmaker.EntitySystem.Entities;

namespace ToyBox.Infrastructure.Utilities;

public static partial class ContextProvider {
    private static bool m_UnitProviderShown = false;
    public static bool BaseUnitEntity(out BaseUnitEntity? unit) {
        unit = CharacterPicker.CurrentUnit;
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red());
            return false;
        }
        string str;
        if (unit != null) {
            str = ": " + $"{unit}".Green().Bold();
        } else {
            str = ": " + SharedStrings.NoneText.Red();
        }
        using (VerticalScope()) {
            var justOpened = false;
            if (UI.DisclosureToggle(ref m_UnitProviderShown, SharedStrings.CurrentlySelectedUnitText + str) && m_UnitProviderShown) {
                justOpened = true;
            }
            if (m_UnitProviderShown) {
                _ = CharacterPicker.OnFilterPickerGUI();
                var didChange = CharacterPicker.OnCharacterPickerGUI();
                unit = CharacterPicker.CurrentUnit;
                m_UnitProviderShown = !didChange || unit == null || justOpened;
            }
        }
        return unit != null;
    }
}
