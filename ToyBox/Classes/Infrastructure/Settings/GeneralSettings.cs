namespace ToyBox.Infrastructure;

internal class GeneralSettings : AbstractSettings {
    private static readonly Lazy<GeneralSettings> m_Instance = new(() => {
        var instance = new GeneralSettings();
        instance.Load();
        return instance;
    });
    public static GeneralSettings Settings {
        get {
            return m_Instance.Value;
        }
    }

    protected override string Name {
        get {
            return "Settings.json";
        }
    }

    public int SelectedTab = 0;


    // Settings Tab

    // - Other
    public LogLevel LogLevel = LogLevel.Info;
    public string UILanguage = "en";
    public int NearbyRange = 25;
    public bool EnableLazyInit = true;
    public bool EnableLogHotkeysToCombatLog = false;

    // - Browser
    public int PageLimit = 25;
    public bool ToggleSearchAsYouType = true;
    public float SearchDelay = 0.3f;
    public bool SearchDescriptions = true;

    // - Blueprints Settings
    public bool EnableThreadedBlueprintLoader = true;
    public int BlueprintsLoaderNumShards = 32;
    public int BlueprintsLoaderChunkSize = 200;
    public int BlueprintsLoaderNumThreads = 4;
    public bool PreloadBlueprints = false;
    public bool UseBPIdCache = true;
    public bool AutomaticallyBuildBPIdCache = true;
    public bool EnableBlueprintPerformancePatches = true;
    public bool ToggleBPsShowDisplayAndInternalName = false;

    // - Inspector
    public bool ToggleInspectorShowNullAndEmptyMembers = false;
    public bool ToggleInspectorShowStaticMembers = true;
    public bool ToggleInspectorShowFieldsOnEnumerable = false;
    public bool ToggleInspectorShowCompilerGeneratedFields = true;
    public bool ToggleInspectorSlimMode = false;
    public int InspectorSearchBatchSize = 20000;
    public int InspectorDrawLimit = 500;
    public float InspectorIndentWidth = 20f;
    public float InspectorNameFractionOfWidth = 0.3f;

    // - UpdateAndIntegrity
    public bool EnableVersionCompatibilityCheck = true;
    public bool EnableFileIntegrityCheck = true;


    // Bag of Tricks

    // - Combat
    public bool EnableMurderHobo = false;

    // - Preview
    public bool EnablePreviewDialogResults = false;
    public bool EnablePreviewDialogConditions = false;

    // - Dialog
    public bool EnableLoveIsFree = false;
    public bool EnableJealousyBegone = false;
    public bool EnableRemoteCompanionDialog = false;
    public bool EnableExCompanionDialog = false;

    // - QoL
    public bool EnableModdedAchievements = true;

    // - Cheats
    public bool HighlightHiddenObjects = false;
    public bool HighlightInFogOfWar = false;
    public bool HighlightHiddenTraps = false;
}
