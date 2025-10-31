using ToyBox.Infrastructure.Utilities;

namespace ToyBox;
public abstract class FeatureWithAction : Feature {
    public virtual void LogExecution(params object?[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
    public abstract void ExecuteAction(params object[] parameter);
}
