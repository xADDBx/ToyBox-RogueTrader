using ToyBox.Infrastructure.Keybinds;

namespace ToyBox;

public interface IToggledWithBinding : IBindableFeature {
    new Hotkey? Keybind {
        get;
        set;
    }
}
