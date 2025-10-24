namespace ToyBox.Infrastructure.Enums;
public enum UnitSelectType {
    Off,
    You,
    Party,
    Friendly,
    Enemies,
    Everyone,
}
public static partial class UnitSelectType_Localizer {
    public static string GetLocalized(this UnitSelectType type) {
        return type switch {
            UnitSelectType.Off => m_OffText,
            UnitSelectType.You => m_MainCharacterText,
            UnitSelectType.Party => m_PartyText,
            UnitSelectType.Friendly => m_FriendlyText,
            UnitSelectType.Enemies => m_EnemiesText,
            UnitSelectType.Everyone => m_EveryoneText,
            _ => "!!Error Unknown UnitSelectType!!",
        };
    }

    [LocalizedString("ToyBox_Infrastructure_Enums_UnitSelectType_Localizer_EveryoneText", "Everyone")]
    private static partial string m_EveryoneText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_UnitSelectType_Localizer_EnemiesText", "Enemies")]
    private static partial string m_EnemiesText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_UnitSelectType_Localizer_FriendlyText", "Friendly")]
    private static partial string m_FriendlyText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_UnitSelectType_Localizer_PartyText", "Party")]
    private static partial string m_PartyText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_UnitSelectType_Localizer_MainCharacterText", "Main Character")]
    private static partial string m_MainCharacterText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_UnitSelectType_Localizer_OffText", "Off")]
    private static partial string m_OffText { get; }
}
