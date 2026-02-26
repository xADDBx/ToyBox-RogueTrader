using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Utilities;

namespace ToyBox;

public abstract class FeatureWithAction : Feature {
    public virtual void LogExecution(params object?[] parameter) {
        Helpers.LogExecution(this, parameter);
    }
    public abstract bool CanExecute(ActionParameter parameter);
    public abstract void ExecuteAction(ActionParameter parameter);
}
