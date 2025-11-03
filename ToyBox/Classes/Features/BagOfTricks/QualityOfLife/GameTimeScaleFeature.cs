using Kingmaker;

namespace ToyBox.Features.BagOfTricks.QualityOfLife;
public partial class GameTimeScaleFeature : FeatureWithFloatSlider {
    public override bool IsEnabled {
        get {
            return Value != 1f;
        }
    }
    public override void Initialize() {
        base.Initialize();
        if (GetInstance<GameAlternateTimeScaleFeature>().IsEnabled) {
            Game.Instance.TimeController.DebugTimeScale = Settings.GameAlternateTimeScaleMultiplier;
        } else {
            Game.Instance.TimeController.DebugTimeScale = Value;
        }
    }
    public override void Destroy() {
        base.Destroy();
        if (GetInstance<GameAlternateTimeScaleFeature>().IsEnabled) {
            Game.Instance.TimeController.DebugTimeScale = Settings.GameAlternateTimeScaleMultiplier;
        } else {
            Game.Instance.TimeController.DebugTimeScale = 1;
        }
    }
    public override ref float Value {
        get {
            return ref Settings.GameTimeScaleMultiplier;
        }
    }
    public override int Digits {
        get {
            return 3;
        }
    }
    public override float Min {
        get {
            return 0.001f;
        }
    }

    public override float Max {
        get {
            return 20f;
        }
    }

    public override float? Default {
        get {
            return 1f;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_GameTimeScaleFeature_Name", "Game Time Scale")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_QualityOfLife_GameTimeScaleFeature_Description", "Speeds up or slows down the entire game (movement, animation, everything)")]
    public override partial string Description { get; }
}
