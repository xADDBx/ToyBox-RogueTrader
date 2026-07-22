namespace ToyBox.Infrastructure.Enums;

public enum PartyTabSectionType {
    None,
    Careers,
    Stats,
    Features,
    Buffs,
    Abilities,
    Mechadendrites,
    Inspect
}
public static partial class PartyTabSectionType_Localizer {
    public static string GetLocalized(this PartyTabSectionType type) {
        return type switch {
            PartyTabSectionType.None => SharedStrings.NoneText,
            PartyTabSectionType.Careers => m_ClassesText,
            PartyTabSectionType.Stats => m_StatsText,
            PartyTabSectionType.Features => m_FeaturesText,
            PartyTabSectionType.Buffs => m_BuffsText,
            PartyTabSectionType.Abilities => m_AbilitiesText,
            PartyTabSectionType.Mechadendrites => m_MechadendritesText,
            PartyTabSectionType.Inspect => m_InspectText,
            _ => "!!Error Unknown PartyTabSectionType!!",
        };
    }
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_ClassesText", "Careers")]
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
    [LocalizedString("ToyBox_Infrastructure_Enums_PartyTabSectionType_Localizer_m_MechadendritesLocalizedText", "Mechadendrites")]
    private static partial string m_MechadendritesText { get; }
}
