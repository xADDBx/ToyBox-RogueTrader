using ToyBox.Classes.Infrastructure.Features;
using ToyBox.Infrastructure.Keybinds;

namespace ToyBox;

public interface IBindableFeature {
    abstract Hotkey? Keybind {
        get;
    }
    abstract void LogExecution(ActionParameter parameter);
    abstract void ExecuteAction(ActionParameter parameter);
}
