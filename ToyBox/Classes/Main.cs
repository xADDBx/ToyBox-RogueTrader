using Kingmaker.Pathfinding;
using System.Collections.Concurrent;
using System.Diagnostics;
using ToyBox.Features.SettingsFeatures.UpdateAndIntegrity;
using ToyBox.Infrastructure.Utilities;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox;
#if DEBUG
[EnableReloading]
#endif
public static partial class Main {
    private const string m_HarmonyId = "ToyBox";
    internal static Harmony HarmonyInstance = new(m_HarmonyId);
    internal static UnityModManager.ModEntry ModEntry = null!;
    internal static List<Task> LateInitTasks = [];
    public static Action? OnLocaleChanged;
    public static Action? OnHideGUIAction;
    public static Action? OnShowGUIAction;
    public static Action? OnUnloadAction;
    private static Exception? m_CaughtException = null;
    internal static List<FeatureTab> m_FeatureTabs = [];
    internal static List<WeakReference<IPagedList>> m_VerticalLists = [];
    private static readonly ConcurrentQueue<Action> m_MainThreadTaskQueue = [];
    private static readonly Lazy<bool> m_SuccessfullyInitialized = new(() => LateInitTasks.Count > 0 && LateInitTasks.All(t => t.IsCompleted));
    public static bool SuccessfullyInitialized {
        get {
            return field || m_SuccessfullyInitialized.Value;
        }
        internal set;
    } = false;
    private static bool Load(UnityModManager.ModEntry modEntry) {
        var sw = Stopwatch.StartNew();
        try {
            ModEntry = modEntry;
            ModEntry.OnUnload = OnUnload;
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnShowGUI = OnShowGUI;
            ModEntry.OnHideGUI = OnHideGUI;
            ModEntry.OnGUI = OnGUI;
            ModEntry.OnUpdate = OnUpdate;
            ModEntry.OnSaveGUI = OnSaveGUI;

            if (Settings.EnableFileIntegrityCheck && !IntegrityCheckerFeature.CheckFilesHealthy()) {
                Critical("Failed Integrity Check. Files have issues!");
                ModEntry.Info.DisplayName = "ToyBox ".Orange().SizePercent(40) + SharedStrings.ModFilesAreCorrupted_Text.Red().Bold().SizePercent(60);
                ModEntry.OnGUI = _ => UpdaterFeature.UpdaterGUI();
                return true;
            }

            if (Settings.EnableVersionCompatibilityCheck) {
                _ = Task.Run(() => {
                    var versionTimer = Stopwatch.StartNew();
                    VersionChecker.IsGameVersionSupported();
                    Debug($"Finished Version Compatibility Check in: {versionTimer.ElapsedMilliseconds}ms (Threaded)");
                });
            }

            var sw2 = Stopwatch.StartNew();
            Infrastructure.Localization.LocalizationManager.Enable();
            Debug($"Localization init took {sw2.ElapsedMilliseconds}ms");

            sw2.Start();
            _ = BPLoader;
            Debug($"BPLoader init took {sw2.ElapsedMilliseconds}ms");

            ToyBoxUnitHelper.Initialize();

            sw2.Start();
            RegisterFeatureTabs();
            Debug($"Early init took {sw2.ElapsedMilliseconds}ms");

            foreach (var tab in m_FeatureTabs) {
                LateInitTasks.Add(Task.Run(tab.InitializeAll));
            }
            LazyInit.Stopwatch.Start();

            LazyInit.EnsureFinish();

        } catch (Exception ex) {
            Error(ex);
            _ = OnUnload(modEntry);
            return false;
        }
        Debug($"Complete init took {sw.ElapsedMilliseconds}ms");
        return true;
    }
    private static string[]? m_TabNames;
    private static void RegisterFeatureTabs() {
        m_FeatureTabs.Add(new Features.BagOfTricks.BagOfTricksFeatureTab());
        m_FeatureTabs.Add(new Features.PartyTab.PartyFeatureTab());
        m_FeatureTabs.Add(new Features.SettingsFeatures.SettingsFeaturesTab());
        m_FeatureTabs.Add(new Features.FeatureSearch.FeatureSearchTab());
        m_FeatureTabs.Add(new Infrastructure.Blueprints.BlueprintActions.BlueprintActions());
    }
    private static bool OnUnload(UnityModManager.ModEntry modEntry) {
        foreach (var tab in m_FeatureTabs) {
            tab.DestroyAll();
        }
        HarmonyInstance.UnpatchAll(m_HarmonyId);
        OnUnloadAction?.Invoke();
        return true;
    }
    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
        return true;
    }

    private static int m_LoadedBps = 0;
    private static void OnGUI(UnityModManager.ModEntry modEntry) {
        if (!SuccessfullyInitialized) {
            UI.Label(m_SomethingWentHorriblyWrongAndYou.Red().Bold());
        }
        if (m_CaughtException == null) {
            try {
                if (BPLoader.IsLoading) {
                    UI.ProgressBar(BPLoader.Progress, "");
                }
                if (ImguiCanChangeStateAtBeginning() && BPLoader.HasLoaded) {
                    m_LoadedBps = BPLoader.GetBlueprints()!.Count;
                }
                Space(10);

                UI.Label(SharedStrings.CurrentlyLoadedBPsText.Format(m_LoadedBps));
                m_TabNames ??= [.. m_FeatureTabs.Where(tab => !tab.IsHiddenFromUI).Select(t => t.Name)];
                Settings.SelectedTab = GUILayout.SelectionGrid(Settings.SelectedTab, m_TabNames, Math.Min(m_TabNames.Length, 10), Width(EffectiveWindowWidth()));
                Space(10);
                Div.DrawDiv();
                Space(10);
                m_FeatureTabs[Settings.SelectedTab].OnGui();
            } catch (Exception ex) {
                Error(ex);
                m_CaughtException = ex;
            }
        } else {
            UI.Label(m_CaughtException.ToString());
            if (UI.Button(SharedStrings.ResetLabel)) {
                m_CaughtException = null;
            }
        }
    }
    private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
        Settings.Save();
        Hotkeys.Save();
    }

    private static void OnShowGUI(UnityModManager.ModEntry modEntry) {
        OnShowGUIAction?.Invoke();
    }

    private static void OnHideGUI(UnityModManager.ModEntry modEntry) {
        Settings.Save();
        Hotkeys.Save();
        OnHideGUIAction?.Invoke();
    }
    private static void OnUpdate(UnityModManager.ModEntry modEntry, float z) {
        try {
            if (m_MainThreadTaskQueue.TryDequeue(out var task)) {
                task();
            }
            Hotkeys.UpdateLoop();
        } catch (Exception ex) {
            Error(ex);
        }
    }
    public static void ScheduleForMainThread(this Action action) {
        m_MainThreadTaskQueue.Enqueue(action);
    }

    [LocalizedString("ToyBox_Main_SomethingWentHorriblyWrongAndYou", "Something went horribly wrong and you've somehow opened the UI before ToyBox finished initialization. Please report this to the mod author!")]
    private static partial string m_SomethingWentHorriblyWrongAndYou { get; }
}
