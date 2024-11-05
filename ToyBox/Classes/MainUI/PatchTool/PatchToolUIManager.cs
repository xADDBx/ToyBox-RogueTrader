using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static class PatchToolUIManager {
    private static List<PatchToolTabUI> instances = new();
    private static int selectedIndex = -1;
    public static void OnGUI() {
        Label("Tabs".localize().Bold(), AutoWidth());
        using (HorizontalScope()) {
            Space(50);
            for (int i = 0; i < instances.Count; i++) {
                using (HorizontalScope()) {
                    var tabName = instances[i].Target.IsNullOrEmpty() ? "New Tab".localize() : instances[i].Target;
                    if (i == selectedIndex) {
                        Label($"[{tabName}]", AutoWidth());
                    } else {
                        ActionButton(tabName, () => {
                            selectedIndex = i;
                        }, AutoWidth());
                    }
                    ActionButton("Close".localize(), () => {
                        instances.RemoveAt(i);
                        if (selectedIndex >= instances.Count) {
                            selectedIndex = instances.Count - 1;
                        }
                    }, AutoWidth());
                }
            }
            ActionButton("+", () => {
                instances.Add(new PatchToolTabUI());
                selectedIndex = instances.Count - 1;
            }, AutoWidth());
        }
        Div();
        Space(20);
        if (selectedIndex >= 0 && selectedIndex < instances.Count) {
            instances[selectedIndex].OnGUI();
        } else {
            Label("No tabs open.".localize());
        }
    }
}
