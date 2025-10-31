using Kingmaker.Cheats;

namespace ToyBox.Features.BagOfTricks.Common;
[NeedsTesting]
public partial class ChangeWeatherFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ChangeWeatherFeature_Name", "Change Weather")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ChangeWeatherFeature_Description", "Sets the time until next weather change to 0.")]
    public override partial string Description { get; }
    public override void ExecuteAction(params object[] parameter) {
        if (IsInGame()) {
            LogExecution(parameter);
            CheatsCommon.ChangeWeather("");
        }
    }
}
