using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Utility.DotNetExtensions;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.Saves;

public partial class BrowseSavesFeature : Feature {
    [LocalizedString("ToyBox_Features_Saves_BrowseSavesFeature_Name", "Browse Saves")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Saves_BrowseSavesFeature_Description", "Allows looking through all available save games and viewing their char name, party level, area, save name and game id")]
    public override partial string Description { get; }
    public Browser<SaveInfo>? SaveBrowser;
    private TimedCache<bool> m_NeedsUpdate = new(() => {
        GetInstance<BrowseSavesFeature>().SaveBrowser = null;
        return false;
    }, 120, true);
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        var saveManager = Game.Instance.SaveManager;
        saveManager.UpdateSaveListIfNeeded(false);
        if (m_NeedsUpdate || SaveBrowser == null) {
            SaveBrowser = new(info => $"{info.Name}{info.Area.AreaName}{info.Description}{info.FileName}{info.GameId}",
                info => $"{info.PlayerCharacterName}, {info.GameSaveTime}", 
                saveManager.m_SavedGames.Where(save => save?.GameId == Game.Instance.Player.GameId), 
                func => func(saveManager.m_SavedGames.NotNull()), overridePageWidth: (int)(EffectiveWindowWidth() * 0.9f));
            SaveBrowser.SetComparer(BlueprintFilter<SimpleBlueprint>.Sorter);
        }
        SaveBrowser.OnGUI(info => {
            using (HorizontalScope()) {
                UI.Label(info.GameId == Game.Instance.Player.GameId ? info.PlayerCharacterName.Orange() : info.PlayerCharacterName, Width(150 * Main.UIScale));
                Space(25);
                UI.Label($"{m_LevelLocalizedText}: {info.PlayerCharacterRank}", Width(50 * Main.UIScale));
                Space(25);
                UI.Label(info.Area.AreaName, Width(300 * Main.UIScale));
                Space(25);
                var tmp = info.GameId;
                UI.TextField(ref tmp, null, Width(250 * Main.UIScale));
                Space(25);
                UI.Label(info.Name.Green());
            }
        });
    }

    [LocalizedString("ToyBox_Features_Saves_BrowseSavesFeature_m_LevelLocalizedText", "Level")]
    private static partial string m_LevelLocalizedText { get; }
}
