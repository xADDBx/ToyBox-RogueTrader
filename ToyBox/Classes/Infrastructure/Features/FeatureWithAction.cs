using ToyBox.Features.PartyTab;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox;
public abstract class FeatureWithAction : Feature {
    public virtual void LogExecution(params object?[] parameter) {
        var toLog = "Executed action " + GetType().Name + "";
        if (parameter?.Length > 0) {
            toLog += " with parameters " + parameter.ToContentString();
        }
        Trace(toLog);
        PartyFeatureTab.FeatureRefresh();
    }
    public abstract void ExecuteAction(params object[] parameter);
}
