using Kingmaker.Cheats;
using ToyBox.Classes.Infrastructure.Features;

namespace ToyBox.Features.BagOfTricks.Common;

[IsTested]
public partial class ChangeWeatherFeature : FeatureWithBindableAction {
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ChangeWeatherFeature_Name", "Change Weather")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Common_ChangeWeatherFeature_Description", "Sets the time until next weather change to 0.")]
    public override partial string Description { get; }
    public override bool CanExecute(ActionParameter parameter) {
        return IsInGame();
    }
    public override void ExecuteAction(ActionParameter parameter) {
        if (CanExecute(parameter)) {
            LogExecution(parameter);
            CheatsCommon.ChangeWeather("");
        }
    }
}
