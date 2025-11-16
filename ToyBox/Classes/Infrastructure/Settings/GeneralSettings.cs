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
    public bool EnableShowRiskyToggles = false;

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
    public bool EnableOverrideStoryOccupation = false;
    public HashSet<string> OverridenOccupations = [];
    public bool EnableIgnoreDialogRestrictionsSoulMark = false;
    public bool EnableIgnoreDialogRestrictionsEverything = false;

    // - QoL
    public bool EnableModdedAchievements = true;
    public bool EnableSkipSplashScreen = false;
    public bool EnableObjectHighlightToggle = false;
    public bool EnableAutoLoadLastSaveOnLaunch = false;
    public bool EnableRefillBeltConsumables = false;
    public bool EnableClickToTransferEntireStack = false;
    public float GameTimeScaleMultiplier = 1f;
    public bool EnableGameAlternateTimeScale = false;
    public float GameAlternateTimeScaleMultiplier = 3f;
    public bool DisableEndTurnKeybindFeature = false;
    public bool EnableLoadingWithBlueprintErrors = false;

    // - RT Specific
    public bool DisableRandomWarpEncounters = false;
    public bool EnablePreventPsychicPhenomena = false;
    public bool EnablePreventVeilThicknessFromChanging = false;
    public bool EnableCustomizePsychicPhenomena = false;
    public HashSet<string> ExcludedRandomPhenomena = [];
    public HashSet<string> ExcludedPerilsMinor = [];
    public HashSet<string> ExcludedPerilsMajor = [];

    // - Camera
    public bool EnableAllowZoomOnAllMapsAndCutscenes = false;
    public bool EnableAllowRotateOnAllMapsAndCutscenes = false;
    public float FOVMultiplierSetting = 1;
    public bool EnableFreeCam = false;
    public float CameraElevationOffset = 0;
    public bool EnableDragCameraElevation = false;

    // - Cheats
    public bool HighlightHiddenObjects = false;
    public bool HighlightInFogOfWar = false;
    public bool HighlightHiddenTraps = false;
}
