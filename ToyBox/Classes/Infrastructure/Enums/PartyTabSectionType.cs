namespace ToyBox.Infrastructure.Enums;
public enum PartyTabSectionType {
    None,
    Classes,
    Stats,
    Features,
    Buffs,
    Abilities,
    Inspect
}
public static partial class PartyTabSectionType_Localizer {
    public static string GetLocalized(this PartyTabSectionType type) {
        return type switch {
            PartyTabSectionType.None => SharedStrings.NoneText,
            PartyTabSectionType.Classes => m_ClassesText,
            PartyTabSectionType.Stats => m_StatsText,
            PartyTabSectionType.Features => m_FeaturesText,
            PartyTabSectionType.Buffs => m_BuffsText,
            PartyTabSectionType.Abilities => m_AbilitiesText,
            PartyTabSectionType.Inspect => m_InspectText,
            _ => "!!Error Unknown PartyTabSectionType!!",
        };
    }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_ClassesText", "Classes")]
    private static partial string m_ClassesText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_StatsText", "Stats")]
    private static partial string m_StatsText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_FeaturesText", "Features")]
    private static partial string m_FeaturesText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_BuffsText", "Buffs")]
    private static partial string m_BuffsText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_AbilitiesText", "Abilities")]
    private static partial string m_AbilitiesText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_InspectText", "Inspect")]
    private static partial string m_InspectText { get; }
}
