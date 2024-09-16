// some stuff borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using ModKit.Utility;
using System;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace ModKit {
    public enum LogLevel : int {
        Error,
        Warning,
        Info,
        Debug,
        Trace
    }

    public static partial class Mod {
        public static ModEntry modEntry { get; set; } = null;
        public static string modEntryPath { get; set; } = null;
        private static ModEntry.ModLogger modLogger;
        public static LogLevel logLevel = LogLevel.Info;
        public delegate void UITranscriptLogger(string text);
        public static UITranscriptLogger InGameTranscriptLogger;

        public static void OnLoad(ModEntry modEntry) {
            modEntry.OnSaveGUI -= OnSaveGUI;
            modEntry.OnSaveGUI += OnSaveGUI;
            Mod.modEntry = modEntry;
            modLogger = modEntry.Logger;
            modEntryPath = modEntry.Path;
            ModKitSettings.Load();
            Debug($"ModKitSettings.browserSearchLimit: {ModKitSettings.browserDetailSearchLimit}");
        }
        public static void OnSaveGUI(ModEntry entry) {
            ModKitSettings.Save();
            LocalizationManager.Export();
        }
        private static void ResetGUI(ModEntry modEntry) => ModKitSettings.Load();
        public static void Error(string str) {
            str = str.Yellow().Bold();
            modLogger?.Error(str + "\n" + new System.Diagnostics.StackTrace(1, true).ToString());
        }
        public static void Error(Exception ex) => Error(ex.ToString());
        public static void Warn(string str) {
            if (logLevel >= LogLevel.Warning)
                modLogger?.Log("[Warn] ".Orange().Bold() + str);
        }
        public static void Log(string str) {
            if (logLevel >= LogLevel.Info)
                modLogger?.Log("[Info] " + str);
        }
        public static void Log(int indent, string s) => Log("    ".Repeat(indent) + s);
        public static void Debug(string str) {
            if (logLevel >= LogLevel.Debug)
                modLogger?.Log("[Debug] ".Green() + str);
        }
        public static void Trace(string str) {
            if (logLevel >= LogLevel.Trace)
                modLogger?.Log("[Trace] ".Color(RGBA.lightblue) + str);
        }

        public delegate void ShowGUINotifierMethod();
        public static ShowGUINotifierMethod NotifyOnShowGUI;

        public static void OnShowGUI() {
            if (NotifyOnShowGUI != null) NotifyOnShowGUI();
        }
    }
}