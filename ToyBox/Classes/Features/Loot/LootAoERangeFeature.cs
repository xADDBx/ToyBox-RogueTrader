namespace ToyBox.Features.Loot;

public partial class LootAoERangeFeature : FeatureWithIntSlider {
    public override bool IsEnabled {
        get {
            return false;
        }
    }

    public override ref int Value {
        get {
            return ref Settings.LootAoERange;
        }
    }

    public override int Min {
        get {
            return 0;
        }
    }

    public override int Max {
        get {
            return 100;
        }
    }

    public override int? Default {
        get {
            return 15;
        }
    }
    [LocalizedString("ToyBox_Features_Loot_LootAoERangeFeature_Name", "AoE Loot Range")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_Loot_LootAoERangeFeature_Description", "Modifies the range (in m) in which the AoE Loot collects the loot.")]
    public override partial string Description { get; }
}
