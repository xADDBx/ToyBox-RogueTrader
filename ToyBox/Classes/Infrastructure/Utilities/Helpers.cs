using Kingmaker;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.LifeEvents;
using Kingmaker.UI.Models.Log.GameLogCntxt;
using ToyBox.Features.PartyTab;
using ToyBox.Features.SettingsFeatures;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox.Infrastructure.Utilities;

public static class Helpers {
    public static bool IsInGame() {
        return Game.Instance.Player?.Party?.Count > 0;
    }
    public static float CalculateLargestLabelSize(IEnumerable<string> items, GUIStyle? style = null) {
        style ??= GUI.skin.label;
        return items.Max(item => style.CalcSize(new(item)).x);
    }
    public static void ToggleModWindow() {
        UnityModManager.UI.Instance.ToggleWindow();
    }
    public static void LogExecution(Feature feature, params object?[] parameter) {
        var toLog = "Executed action " + feature.Name;
        if (parameter?.Length > 0) {
            toLog += " with parameters " + parameter.ToContentString();
        }
        OwlLog(toLog);

        if (Feature.GetInstance<LogHotkeysToCombatLogSetting>().IsEnabled) {
            var messageText = "ToyBox".Blue() + " - " + toLog;
            var message = new CombatLogMessage(messageText, Color.black, Kingmaker.UI.Models.Log.Enums.PrefixIcon.RightArrow);
            var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Dialog].FirstOrDefault(x => x is DialogLogThread);
            using (GameLogContext.Scope) {
                messageLog?.AddMessage(message);
            }
        }

        PartyFeatureTab.FeatureRefresh();
    }
    public static Vector3 GetCursorPositionInWorld() {
        var camera = Game.GetCamera();
        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var raycastHit, camera.farClipPlane, 21761)) {
            return raycastHit.point;
        }
        return default;
    }
    public static SaveSpecificSettings? InSaveSettings {
        get {
            return SaveSpecificSettings.Instance;
        }
    }
}
