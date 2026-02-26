using ToyBox.Infrastructure.Keybinds;

namespace ToyBox;

public interface IToggleWithPseudoBinding : IToggledWithBinding { }
public interface IToggledWithBinding : IBindableFeature {
    new Hotkey? Keybind {
        get;
        set;
    }
}
