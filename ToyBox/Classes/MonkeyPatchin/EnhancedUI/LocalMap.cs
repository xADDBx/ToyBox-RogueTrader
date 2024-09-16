using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Code.UI.MVVM.View.Overtips.Unit;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.Common.Markers;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.PC;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace ToyBox.BagOfPatches {
    internal static class LocalMapPatches {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        public static float Zoom = 1.0f;
        public static float Width = 0.0f;
        public static Vector3 Position = Vector3.zero;
        public static Vector3 FrameRotation = Vector3.zero;
        [HarmonyPatch(typeof(LocalMapVM))]
        internal static class LocalMapVMPatch {
            [HarmonyPatch(nameof(LocalMapVM.SetMarkers))]
            [HarmonyPrefix]
            private static bool SetMarkers(LocalMapVM __instance) {
                Mod.Debug($"LocalMapVM.SetMarkers");
                LocalMapModel.Markers.RemoveWhere(m => m.GetMarkerType() == LocalMapMarkType.Invalid);
                foreach (var marker in LocalMapModel.Markers)
                    if (LocalMapModel.IsInCurrentArea(marker.GetPosition()))
                        __instance.MarkersVm.Add(new LocalMapCommonMarkerVM(marker));
                IEnumerable<BaseUnitEntity> first = Game.Instance.Player.PartyAndPets;
                if (Game.Instance.Player.CapitalPartyMode)
                    first = first.Concat(Game.Instance.Player.RemoteCompanions.Where(u => !u.IsCustomCompanion()));
                foreach (var unit in first)
                    if (unit.View != null
                        && unit.View.enabled
                        && !unit
                            .LifeState
                            .IsHiddenBecauseDead
                        && LocalMapModel.IsInCurrentArea(unit.Position)
                        ) {
                        __instance.MarkersVm.Add(new LocalMapCharacterMarkerVM(unit));
                        __instance.MarkersVm.Add(new LocalMapDestinationMarkerVM(unit));
                    }

                foreach (var units in Shodan.MainCharacter
                                            .CombatGroup
                                            .Memory.UnitsList) {
                    Mod.Debug($"Checking {units.Unit.CharacterName}");
                    if (!units.Unit.IsPlayerFaction
                        && (units.Unit.IsVisibleForPlayer || units.Unit.InterestingnessCoefficent() > 0)
                        && !units.Unit.Descriptor()
                                 .LifeState.IsDead
                        && LocalMapModel.IsInCurrentArea(units.Unit.Position)
                       ) {
                        __instance.MarkersVm.Add(new LocalMapUnitMarkerVM(units));
                    }
                }
                return false;
            }
        }



        [HarmonyPatch(typeof(LocalMapMarkerPCView), nameof(LocalMapMarkerPCView.BindViewImplementation))]
        private static class LocalMapMarkerPCView_BindViewImplementation_Patch {
            [HarmonyPostfix]
            public static void Postfix(LocalMapMarkerPCView __instance) {
                if (__instance == null)
                    return;
                //Mod.Debug($"LocalMapMarkerPCView.BindViewImplementation - {__instance.ViewModel.MarkerType} - {__instance.ViewModel.GetType().Name}");
                if (__instance.ViewModel.MarkerType == LocalMapMarkType.Loot)
                    __instance.AddDisposable(__instance.ViewModel.IsVisible.Subscribe(value => {
                        (__instance as LocalMapLootMarkerPCView)?
                            .gameObject.SetActive(value);
                    }));
            }

            // Helper Function - Not a Patch
            private static void UpdateMarker(LocalMapMarkerPCView markerView, BaseUnitEntity unit) {
                var count = unit.InterestingnessCoefficent();
                //Mod.Debug($"{unit.CharacterName.orange()} -> unit interestingness: {count}");
                //var attentionMark = markerView.transform.Find("ToyBoxAttentionMark")?.gameObject;
                //Mod.Debug($"attentionMark: {attentionMark}");
                var markImage = markerView.transform.Find("Mark").GetComponent<Image>();
                if (count >= 1) {
                    //Mod.Debug($"adding Mark to {unit.CharacterName.orange()}");
                    var mark = markerView.transform;
                    markImage.color = new Color(1, 1f, 0);
                } else {
                    //                    attentionMark?.SetActive(false);
                    markImage.color = new Color(1, 1, 1);
                }
            }
        }

        [HarmonyPatch(typeof(OvertipUnitView))]
        private static class OvertipUnitView_Patch {
            [HarmonyPatch(nameof(OvertipUnitView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(OvertipUnitView __instance) {
                if (!Settings.toggleShowInterestingNPCsOnLocalMap) return;
            }
            [HarmonyPatch(nameof(OvertipUnitView.UpdateVisibility))]
            [HarmonyPostfix]
            public static void UpdateInternal(OvertipUnitView __instance) {
                if (!Settings.toggleShowInterestingNPCsOnLocalMap || __instance is null) return;
            }
        }
    }
}