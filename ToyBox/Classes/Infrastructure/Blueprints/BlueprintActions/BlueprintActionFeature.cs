using Kingmaker.Blueprints;
using ToyBox.Classes.Infrastructure.Blueprints.BlueprintActions.Units;
using ToyBox.Infrastructure.Blueprints.BlueprintActions.Units;

namespace ToyBox.Infrastructure.Blueprints.BlueprintActions;

public interface IExecutableAction<in T> where T : SimpleBlueprint {
    // Null - Nothing happened; False - Action execution failed; True - Action execution succeeded
    abstract bool? OnGui(T blueprint, bool isFeatureSearch, params object[] parameter);
}
public interface IBlueprintAction<T> : IExecutableAction<T>, INeedContextFeature<T> where T : SimpleBlueprint {
    abstract bool CanExecute(T blueprint, params object[] parameter);
}
public abstract class BlueprintActionFeature : FeatureWithAction {
    private static readonly List<object> m_AllActions = [];
    private static readonly Dictionary<Type, object> m_ActionsForType = [];
    private static readonly Dictionary<(Type, Type), object> m_ExactActionsForType = [];
    public override void ExecuteAction(params object[] parameter) {
        LogExecution(parameter);
    }
    protected BlueprintActionFeature() {
        m_AllActions.Add(this);
    }
    private static IEnumerable<IExecutableAction<T>> GetAllActionsForBPType<T>() where T : SimpleBlueprint {
        if (m_ActionsForType.TryGetValue(typeof(T), out var actions)) {
            return (List<IExecutableAction<T>>)actions;
        } else {
            List<IExecutableAction<T>> newActions = [];
            foreach (var action in m_AllActions) {
                if (action is IExecutableAction<T> typedAction) {
                    newActions.Add(typedAction);
                }
            }
            m_ActionsForType[typeof(T)] = newActions;
            return newActions;
        }
    }
    public static IEnumerable<IExecutableAction<T>> GetActionsForBlueprintType<T>(Type? exactlyThis) where T : SimpleBlueprint {
        if (exactlyThis == null) {
            return GetAllActionsForBPType<T>();
        } else {
            if (m_ExactActionsForType.TryGetValue((typeof(T), exactlyThis), out var actions)) {
                return (List<IExecutableAction<T>>)actions;
            } else {
                List<IExecutableAction<T>> newActions = [];
                foreach (var action in GetAllActionsForBPType<T>()) {
                    var interf = action.GetType().GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExecutableAction<>));
                    if (interf.GetGenericArguments()[0] == exactlyThis) {
                        newActions.Add(action);
                    }
                }
                m_ExactActionsForType[(typeof(T), exactlyThis)] = newActions;
                return newActions;
            }
        }
    }
    protected static string StyleActionString(string text, bool isFeatureSearch) {
        if (isFeatureSearch) {
            return text.Cyan().Bold().SizePercent(115);
        } else {
            return text;
        }
    }
}
public class BlueprintActions : FeatureTab {
    public override string Name {
        get {
            return "Blueprint Actions (you should not be seeing this!)";
        }
    }

    public override bool IsHiddenFromUI {
        get {
            return true;
        }
    }

    public BlueprintActions() {
#warning TODO: BlueprintSelection; BlueprintSelectionFeature
        AddFeature(new ColonizeColonyBA());
        AddFeature(new ColonizePlanetBA());
        AddFeature(new LoadAreaPresetBA());
        AddFeature(new TeleportBlueprintAreaBA());
        AddFeature(new TeleportBlueprintAreaEnterPointBA());
        AddFeature(new TeleportBlueprintSectorMapPointStarSystemBA());
        AddFeature(new TeleportBlueprintStarSystemMapBA());

        AddFeature(new AddItemBA());
        AddFeature(new RemoveItemBA());

        AddFeature(new ChangeFlagValueBA());
        AddFeature(new CompleteEtudeBA());
        AddFeature(new CompleteQuestBA());
        AddFeature(new CompleteQuestObjectiveBA());
        AddFeature(new LockFlagBA());
        AddFeature(new PlayCutsceneBA());
        AddFeature(new StartEtudeBA());
        AddFeature(new StartQuestBA());
        AddFeature(new StartQuestObjectiveBA());
        AddFeature(new UnlockAchievementBA());
        AddFeature(new UnlockFlagBA());
        AddFeature(new UnstartEtudeBA());

        AddFeature(new AddAbilityResourceBA());
        AddFeature(new AddMechadendriteBA());
        AddFeature(new AddMechanicEntityFactBA());
        AddFeature(new ChangeBuffRankBA());
        AddFeature(new ChangeFeatureRankBA());
        AddFeature(new ChangeVoiceBA());
        AddFeature(new PlayVoiceBA());
        AddFeature(new RemoveAbilityResourceBA());
        AddFeature(new RemoveMechadendriteBA());
        AddFeature(new RemoveMechanicEntityFactBA());
        AddFeature(new SpawnUnitBA());
    }
}
