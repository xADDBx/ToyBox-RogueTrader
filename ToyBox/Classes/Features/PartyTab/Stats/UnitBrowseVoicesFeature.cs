using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.Visual.Sound;
using ToyBox.Classes.Infrastructure.Blueprints.BlueprintActions.Units;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Stats;

public partial class UnitBrowseVoicesFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitBrowseVoicesFeature_Name", "Browse Voices")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitBrowseVoicesFeature_Description", "Allows browsing and changing the different voices on a unit.")]
    public override partial string Description { get; }
    private Browser<BlueprintUnitAsksList>? m_CachedBrowser;
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }
    public override void OnGui() {
        if (GetContext(out var unit)) {
            OnGui(unit!);
        }
    }
    private bool m_ShowBlueprintVoicePicker = false;
    public void OnGui(BaseUnitEntity unit) {
        UI.DisclosureToggle(ref m_ShowBlueprintVoicePicker, m_ShowBlueprintVoicePickerLocalizedText);
        if (m_ShowBlueprintVoicePicker) {
            UI.Label(m_TheButton_1_WillPlayTryToPlayARaLocalizedText.Format(GetInstance<PlayVoiceBA>().Name).Green());
            if (unit.Asks.List != null) {
                if (!unit.IsMainCharacter && !unit.IsCustomCompanion()) {
                    UI.Label(m_ChangingTheVoiceOfANon_customChaLocalizedText.Red());
                } else if (!BPHelper.GetTitle(unit.Asks.List).StartsWith("RT")) {
                    UI.Label(m_UsingANon_defaultVoiceToACustomCLocalizedText.Red());
                }
            }
            using (HorizontalScope()) {
                if (m_CachedBrowser == null) {
                    m_CachedBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => BPLoader.GetBlueprintsOfType(func), overridePageWidth: (int)(EffectiveWindowWidth() - (50 * Main.UIScale)));
                    BPLoader.GetBlueprintsOfType<BlueprintUnitAsksList>(bps => m_CachedBrowser.QueueUpdateItems(bps.Where(bp => BPHelper.GetTitle(bp).StartsWith("RT"))));
                }
                m_CachedBrowser.OnGUI(voice => {
                    BlueprintUI.BlueprintRowGUI(voice, unit);
                });
            }
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitBrowseVoicesFeature_m_ShowBlueprintVoicePickerLocalizedText", "Show Blueprint Voice Picker")]
    private static partial string m_ShowBlueprintVoicePickerLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitBrowseVoicesFeature_m_ChangingTheVoiceOfANon_customChaLocalizedText", "Changing the voice of a non-custom character is not tested.")]
    private static partial string m_ChangingTheVoiceOfANon_customChaLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitBrowseVoicesFeature_m_UsingANon_defaultVoiceToACustomCLocalizedText", "Using a non-default voice on a custom character is not tested.")]
    private static partial string m_UsingANon_defaultVoiceToACustomCLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitBrowseVoicesFeature_m_TheButton_1_WillPlayTryToPlayARaLocalizedText", "The button {1} will play try to play a random PartyMemberUnconscious sound.")]
    private static partial string m_TheButton_1_WillPlayTryToPlayARaLocalizedText { get; }
}
