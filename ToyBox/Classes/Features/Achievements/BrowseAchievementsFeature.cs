using Kingmaker;
using Kingmaker.Achievements;
using Kingmaker.Stores;

namespace ToyBox.Features.Achievements;

[IsTested]
public partial class BrowseAchievementsFeature : Feature {
    [LocalizedString("ToyBox_Features_Achievements_BrowseAchievementsFeature_Name", "Browse Achievements")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Achievements_BrowseAchievementsFeature_Description", "Once a save is loaded, this allows looking through a list of achievements, supporting inspecting and unlocking them.")]
    public override partial string Description { get; }
    private Browser<AchievementData>? m_AchievementsBrowser;
    private static List<AchievementData>? m_AllAchievements;
    private static string m_LastGameId = "";
    private static TimeSpan m_LastGameTime = TimeSpan.MaxValue;
    public override void OnGui() {
        if (!IsInGame()) {
            UI.Label(SharedStrings.ThisCannotBeUsedFromTheMainMenu.Red().Bold());
            return;
        }
        if (m_AchievementsBrowser == null) {
            m_AllAchievements = [.. Game.Instance.BlueprintRoot.Achievements.List.Where(ach => {
                var toCheck = StoreManager.Store switch {
                    StoreType.Steam => ach.SteamId,
                    StoreType.GoG => ach.GogId,
                    StoreType.EpicGames => ach.EGSId,
                    StoreType.XboxOne => ach.XboxLiveId,
                    StoreType.XboxSeries => ach.XboxLiveId,
                    _ => ach.SteamId
                };
                return !string.IsNullOrWhiteSpace(toCheck);
            })];
            m_AchievementsBrowser = new(BPHelper.GetSortKey, BPHelper.GetSearchKey, null, func => func(m_AllAchievements!), overridePageWidth: (int)(EffectiveWindowWidth() - (40 * Main.UIScale)));
        }
        if (Game.Instance.Player.GameId != m_LastGameId || Game.Instance.Player.GameTime < m_LastGameTime) {
            m_LastGameId = Game.Instance.Player.GameId;
            m_LastGameTime = Game.Instance.Player.GameTime;
            m_AchievementsBrowser.UpdateItems(m_AllAchievements.Where(data => Game.Instance.Player.Achievements.m_Achievements?.Where(ach => ach.IsUnlocked && ach.Data == data).Any() ?? false));
        }
        m_AchievementsBrowser.OnGUI(ach => {
            BlueprintUI.BlueprintRowGUI(m_AchievementsBrowser, ach, null, null, 
                (bp, unit) => Game.Instance.Player.Achievements.m_Achievements?.FirstOrDefault(ach => ach.IsUnlocked && ach.Data == bp));
        }, BlueprintUI.BlueprintHeaderGUI);
    }
}
