using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool;

[HarmonyPatch]
public static class PatchToolPatches {
    private static bool Initialized = false;
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(typeof(StartGameLoader), nameof(StartGameLoader.LoadPackTOC)), HarmonyPostfix]
    public static void Init_Postfix() {
        try {
            if (Initialized) {
                ModKit.Mod.Log("Already initialized blueprints cache.");
                return;
            }
            Initialized = true;

            ModKit.Mod.Log("Patching blueprints.");
            Patcher.PatchAll();
        } catch (Exception e) {
            ModKit.Mod.Log(string.Concat("Failed to initialize.", e));
        }
    }
}
