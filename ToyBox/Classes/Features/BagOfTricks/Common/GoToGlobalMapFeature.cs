using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Persistence;

namespace ToyBox.Features.BagOfTricks.Common;
public partial class GoToGlobalMapFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_GoToGlobalMapFeature_Name", "Go To Global Map")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_GoToGlobalMapFeature_Description", "Tries to load the sector map area.")]
    public override partial string Description { get; }

    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            var globalMap = BlueprintRoot.Instance.SectorMapArea;
            var areaEnterPoint = globalMap.SectorMapEnterPoint;
            LogExecution(areaEnterPoint);
            Game.LoadArea(globalMap, areaEnterPoint, AutoSaveMode.None);
        }
    }
}
