using Kingmaker.EntitySystem.Stats.Base;

namespace ToyBox.Infrastructure;

public static class Constants {
    public const string LinkToIncompatibilitiesFile = "https://raw.githubusercontent.com/xADDBx/ToyBox-RogueTrader/main/ToyBox/ModDetails/Incompatibilities.json";
    public const string SaveFileKey = "ToyBox.SaveSpecificSettings";
    public static readonly HashSet<StatType> LegacyStats = [StatType.CheckBluff, StatType.CheckDiplomacy, StatType.CheckIntimidate, StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill];
    public static readonly HashSet<StatType> WeirdStats = [StatType.TemporaryHitPoints, StatType.Unknown, StatType.AttackOfOpportunityCount,
                                                                StatType.DamageNonLethal, 
                                                                // Those two stats only exist on armor and destructible objects
                                                                StatType.DamageDeflection, StatType.DamageAbsorption];
    public static readonly HashSet<StatType> StarshipStats = [StatType.Crew, StatType.TurretRadius, StatType.TurretRating, StatType.MilitaryRating,
                                                                StatType.PsyRating, StatType.Evasion, StatType.MachineTrait, StatType.ArmourFore,
                                                                StatType.ArmourPort, StatType.ArmourStarboard, StatType.ArmourAft, StatType.Inertia,
                                                                StatType.Power, StatType.Aiming, StatType.RevealRadius, StatType.DetectionRadius,
                                                                StatType.ShieldsAmount, StatType.ShieldsRegeneration, StatType.Morale, StatType.Discipline,
                                                                StatType.InspirationInitialAmount, StatType.InspirationRegeneration];
}
