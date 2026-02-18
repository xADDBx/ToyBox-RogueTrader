using Kingmaker;
using Kingmaker.PubSubSystem.Core;

namespace ToyBox.Features.BagOfTricks.Cheats;

[IsTested]
public partial class RestoreSpellsAndSkillsAfterCombatFeature : ToggledFeature, IPartyCombatHandler {
    public override ref bool IsEnabled {
        get {
            return ref Settings.EnableRestoreSpellsAndSkillsAfterCombat;
        }
    }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_RestoreSpellsAndSkillsAfterCombatFeature_Name", "Restore Spells and Skills after Combat")]
    public override partial string Name { get; }
    [LocalizedString("ToyBox_Features_BagOfTricks_Cheats_RestoreSpellsAndSkillsAfterCombatFeature_Description", "Restores all ability resources once the party leaves combat.")]
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
            foreach (var u in Game.Instance.Player.PartyAndPets) {
                foreach (var resource in u.AbilityResources) {
                    u.AbilityResources.Restore(resource);
                }
                u.Brain.RestoreAvailableActions();
            }
        }
    }
}
