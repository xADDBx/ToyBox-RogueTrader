using ToyBox.Infrastructure.Keybinds;

namespace ToyBox;

public interface IBindableFeature {
    abstract Hotkey? Keybind {
        get;
    }
    abstract void LogExecution(params object[] parameter);
    abstract void ExecuteAction(params object[] parameter);
}
