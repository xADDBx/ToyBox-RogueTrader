using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.Utility.DotNetExtensions;
using ToyBox.Infrastructure.Utilities;
using Warhammer.SpaceCombat.StarshipLogic;

namespace ToyBox.Features.PartyTab.Stats;

[IsTested]
public partial class UnitModifySoulMarksFeature : Feature, INeedContextFeature<BaseUnitEntity> {
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifySoulMarksFeature_Name", "Modify Soul Marks")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifySoulMarksFeature_Description", "Allows modifying the alignment of the character.")]
    public override partial string Description { get; }
    public bool GetContext(out BaseUnitEntity? context) {
        return ContextProvider.BaseUnitEntity(out context);
    }

    public override void OnGui() {
        if (GetContext(out var unit)) {
            using (HorizontalScope()) {
                OnGui(unit!);
            }
        }
    }
    public void OnGui(BaseUnitEntity unit) {
        if (!unit.IsStarship()) {
            if (unit.IsMainCharacter) {
                using (HorizontalScope()) {
                    Space(100);
                    UI.Label(m_SoulMarksLocalizedText.Cyan());
                    using (VerticalScope()) {
                        foreach (SoulMarkDirection direction in Enum.GetValues(typeof(SoulMarkDirection))) {
                            if (direction == SoulMarkDirection.None || direction == SoulMarkDirection.Reason) {
                                continue;
                            }
                            SoulMark? soulMark = null;
                            try {
                                soulMark = SoulMarkShiftExtension.GetSoulMarkFor(unit, direction);
                            } catch (Exception ex) {
                                Error(ex);
                            }
                            if (soulMark != null) {
                                using (HorizontalScope()) {
                                    Space(10);
                                    UI.Label(UIUtility.GetSoulMarkDirectionText(direction), Width(80 * Main.UIScale));
                                    if (soulMark.Rank > 0) {
                                        _ = UI.Button("<", () => {
                                            ModifySoulmark(direction, soulMark, unit, soulMark.Rank - 1, soulMark.Rank - 2);
                                        });
                                    }
                                    UI.Label($" {soulMark.Rank - 1} ".Bold().Orange());
                                    _ = UI.Button(">", () => {
                                        ModifySoulmark(direction, soulMark, unit, soulMark.Rank - 1, soulMark.Rank);
                                    });
                                    Space(10);
                                    var val = soulMark.Rank - 1;
                                    UI.TextField(ref val, pair => {
                                        if (pair.newContent >= -1) {
                                            ModifySoulmark(direction, soulMark, unit, soulMark.Rank - 1, pair.newContent);
                                        }
                                    }, Width(75 * Main.UIScale));
                                }
                            }
                        }
                    }
                }
            } else {
                UI.Label(m_UnitsOtherThanTheMainCharacterDoLocalizedText);
            }
        }
    }
    private static void ModifySoulmark(SoulMarkDirection dir, SoulMark soulMark, BaseUnitEntity ch, int oldRank, int newRank) {
        var change = newRank - oldRank;
        if (change > 0) {
            var soulMarkShift = new SoulMarkShift() { CheckByRank = false, Direction = dir, Value = change };
            new BlueprintAnswer() { SoulMarkShift = soulMarkShift }.ApplyShiftDialog();
        } else if (change < 0) {
            var soulMarkShift = new SoulMarkShift() { CheckByRank = false, Direction = dir, Value = change };
            var provider = new BlueprintAnswer() { SoulMarkShift = soulMarkShift };
            var source = provider as BlueprintScriptableObject;
            var entityFactSource = new EntityFactSource(source, new int?(change));
            if (!soulMark.Sources.ToList().HasItem(entityFactSource)) {
                soulMark.AddSource(source, change);
                soulMark.RemoveRank(-change);
            }
            Game.Instance.DialogController.SoulMarkShifts.Add(provider.SoulMarkShift);
            EventBus.RaiseEvent(delegate (ISoulMarkShiftHandler h) {
                h.HandleSoulMarkShift(provider);
            }, true);
        }
    }

    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifySoulMarksFeature_m_UnitsOtherThanTheMainCharacterDoLocalizedText", "Units other than the main character don't really have an alignment. Assign the milestone features like SoulMarkCorruption2_Feature, though they might not work on other units.")]
    private static partial string m_UnitsOtherThanTheMainCharacterDoLocalizedText { get; }
    [LocalizedString("ToyBox_Features_PartyTab_Stats_UnitModifySoulMarksFeature_m_SoulMarksLocalizedText", "Soul Marks")]
    private static partial string m_SoulMarksLocalizedText { get; }
}
