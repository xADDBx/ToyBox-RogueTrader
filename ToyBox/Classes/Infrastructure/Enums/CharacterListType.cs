namespace ToyBox.Infrastructure.Enums;
public enum CharacterListType {
    Party,
    PartyAndPets,
    AllCharacters,
    Active,
    Remote,
    CustomCompanions,
    Pets,
    Nearby,
    Friendly,
    Enemies,
    AllUnits
}
public static partial class CharacterListType_Localizer {
    public static string GetLocalized(this CharacterListType type) {
        return type switch {
            CharacterListType.Party => m_PartyText,
            CharacterListType.PartyAndPets => m_PartyAndPetsText,
            CharacterListType.AllCharacters => m_AllText,
            CharacterListType.Active => m_ActiveText,
            CharacterListType.Remote => m_RemoteText,
            CharacterListType.CustomCompanions => m_CustomCompanionText,
            CharacterListType.Pets => m_PetsText,
            CharacterListType.Nearby => m_NearbyText,
            CharacterListType.Friendly => m_FriendlyText,
            CharacterListType.Enemies => m_EnemiesText,
            CharacterListType.AllUnits => m_AllUnitsText,
            _ => "!!Error Unknown CharacterList!!",
        };
    }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_PartyText", "Party")]
    private static partial string m_PartyText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_PartyAndPetsText", "Party & Pets")]
    private static partial string m_PartyAndPetsText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_AllText", "All Characters")]
    private static partial string m_AllText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_ActiveText", "Active")]
    private static partial string m_ActiveText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_RemoteText", "Remote")]
    private static partial string m_RemoteText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_CustomCompanionText", "Custom Companions")]
    private static partial string m_CustomCompanionText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_PetsText", "Pets")]
    private static partial string m_PetsText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_NearbyText", "Nearby")]
    private static partial string m_NearbyText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_FriendlyText", "Friendly")]
    private static partial string m_FriendlyText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_EnemiesText", "Enemies")]
    private static partial string m_EnemiesText { get; }
    [LocalizedString("ToyBox_Infrastructure_Enums_CharacterListType_Localizer_AllUnitsText", "All Units")]
    private static partial string m_AllUnitsText { get; }
}
