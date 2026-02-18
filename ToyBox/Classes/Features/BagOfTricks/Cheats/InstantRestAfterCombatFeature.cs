using Kingmaker.Cheats;
using Kingmaker.PubSubSystem.Core;

namespace ToyBox.Features.BagOfTricks.Cheats;

[IsTested]
public partial class InstantRestAfterCombatFeature : ToggledFeature, IPartyCombatHandler {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableInstantRestAfterCombat;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InstantRestAfterCombatFeature_Name", "Instant Rest after Combat")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_InstantRestAfterCombatFeature_Description", "Restores Item charges and rests Units once the party leaves combat.")]
    public override partial string Description { get; }
    private bool m_IsSubscribed = false;
    public override void Enable() {
        base.Enable();
        if (IsEnabled && !m_IsSubscribed) {
            _ = EventBus.Subscribe(this);
            m_IsSubscribed = true;
        }
    }
    public override void Disable() {
        base.Disable();
        if (m_IsSubscribed) {
            EventBus.Unsubscribe(this);
            m_IsSubscribed = false;
        }
    }
    public void HandlePartyCombatStateChanged(bool inCombat) {
        if (!inCombat) {
            CheatsCombat.RestAll();
        }
    }
}
