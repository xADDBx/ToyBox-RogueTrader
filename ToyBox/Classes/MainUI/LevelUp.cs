using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System;
using System.Linq;
using static ModKit.UI;

namespace ToyBox {
    public class LevelUp {
        public static Settings Settings => Main.Settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            Label("This area is under construction.\n".Yellow().Bold() + "As I play the game more it will get flushed out.  For now you see some of the anticipated features along side ones that work".Orange());
            Div(0, 25);
            HStack("Create & Level Up".localize(), 1,
                () => { },
                () => {
                    if (Toggle("Respec from Level 0".localize(), ref Settings.toggleSetDefaultRespecLevelZero, 300.width())) {
                        Settings.toggleSetDefaultRespecLevelFifteen &= !Settings.toggleSetDefaultRespecLevelZero;
                        Settings.toggleSetDefaultRespecLevelThirtyfive &= !Settings.toggleSetDefaultRespecLevelZero;
                    }
                    Label("This allows rechosing the first arcehtype. Also makes Companion respec start from level 0.".Green().localize());
                },
                () => {
                    if (Toggle("Respec from Level 15".localize(), ref Settings.toggleSetDefaultRespecLevelFifteen, 300.width())) {
                        Settings.toggleSetDefaultRespecLevelZero &= !Settings.toggleSetDefaultRespecLevelFifteen;
                        Settings.toggleSetDefaultRespecLevelThirtyfive &= !Settings.toggleSetDefaultRespecLevelFifteen;
                    }
                    Label("This allows rechosing the second archetype.".Green());
                },
                () => {
                    if (Toggle("Respec from Level 35".localize(), ref Settings.toggleSetDefaultRespecLevelThirtyfive, 300.width())) {
                        Settings.toggleSetDefaultRespecLevelZero &= !Settings.toggleSetDefaultRespecLevelThirtyfive;
                        Settings.toggleSetDefaultRespecLevelFifteen &= !Settings.toggleSetDefaultRespecLevelThirtyfive;
                    }
                    Label("This allows rechosing the third archetype.".Green());
                },
                () => Toggle("Ignore Archetypes Prerequisites".localize(), ref Settings.toggleIgnoreCareerPrerequisites),
                () => Toggle("Ignore Talent Prerequisites".localize(), ref Settings.toggleFeaturesIgnorePrerequisites),
                () => Toggle("Ignore Required Stat Values".localize(), ref Settings.toggleIgnorePrerequisiteStatValue),
                () => Toggle("Ignore Required Class Levels".localize(), ref Settings.toggleIgnorePrerequisiteClassLevel),
                () => { }
                );
        }
    }
}
