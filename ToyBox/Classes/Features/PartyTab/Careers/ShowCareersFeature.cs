using Kingmaker.EntitySystem.Entities;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox.Features.PartyTab.Careers;

[IsTested]
public partial class ShowCareersFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ShowCareersFeature_Name", "Show Careers")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ShowCareersFeature_Description", "Lists the careers of a unit and their current level.")]
    public override partial string Description { get; }

    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }

    public void OnGui(BaseUnitEntity unit) {
        UI.Label((m_CareersLocalizedText + ":").Cyan());
        Space(10);
        using (VerticalScope()) {
            foreach (var (blueprint, rank) in unit.Progression.AllCareerPaths) {
                using (HorizontalScope()) {
                    UI.Label(BPHelper.GetTitle(blueprint).Orange() + (" (" + m_LevelLocalizedText + " " + rank + ")").Green());
                }
            }
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Careers_ShowCareersFeature_m_LevelLocalizedText", "Level")]
    private static partial string m_LevelLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Careers_ShowCareersFeature_m_CareersLocalizedText", "Careers")]
    private static partial string m_CareersLocalizedText { get; }
}
