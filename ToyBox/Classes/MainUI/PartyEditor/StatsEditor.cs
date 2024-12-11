using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.Sound;
using ModKit;
using ModKit.Utility;
using ModKit.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ToyBox.classes.Infrastructure;
using Unity.Collections;
using UnityEngine;
using static Kingmaker.Items.WeaponStatsHelper;

namespace ToyBox {

    public partial class PartyEditor {

        public class ToyBoxAlignmentProvider : IAlignmentShiftProvider {
            AlignmentShift IAlignmentShiftProvider.AlignmentShift => new() { Description = "ToyBox Party Editor".LocalizedStringInGame() };
        }

        public static IAlignmentShiftProvider ToyboxAlignmentProvider => new ToyBoxAlignmentProvider();

        public static Dictionary<string, SkeletonReplacer> skeletonReplacers = new();
        public static Dictionary<string, float> lastScaleSize = new();
        private static readonly Dictionary<string, PortraitData> _portraitsByID = new();
        private static bool _portraitsLoaded = false;
        private static Browser<string, string> portraitBrowser;
        private static Browser<BlueprintPortrait, BlueprintPortrait> blueprintPortraitBrowser;
        private static Browser<BlueprintUnitAsksList, BlueprintUnitAsksList> blueprintVoiceBrowser;
        private static bool listCustomPortraits = false;
        private static bool listCustomVoices = false;
        private static bool listBlueprintPortraits = false;
        private static bool listSkeletonOffsets = false;
        private static bool listSkeletonScales = false;
        private static bool listSkeletonSizes = false;
        private static bool listSkeletonItems = false;
        private static IEnumerable<BlueprintPortrait> blueprintPortraitBps = null;
        private static IEnumerable<BlueprintUnitAsksList> blueprintVoiceBps = null;
        private static string newPortraitName = "";
        private static BlueprintPortrait newBlueprintPortrait = null;
        private static bool unknownID = false;

        public static void UnloadPortraits(bool force = false) {
            if (!force && !_portraitsLoaded) return;
            _portraitsByID.Clear();
            _portraitsLoaded = false;
            portraitBrowser = null;
            blueprintPortraitBrowser = null;
            CustomPortraitsManager.Instance.Cleanup();
        }
        public static PortraitData LoadCustomPortrait(string customID, out bool loaded) {
            loaded = false;
            try {
                PortraitData portraitData;
                if (!_portraitsByID.TryGetValue(customID, out portraitData)) {
                    portraitData = new PortraitData(customID);
                    if (portraitData.DirectoryExists()) {
                        _portraitsByID[customID] = CustomPortraitsManager.CreatePortraitData(customID);
                        loaded = true;
                        return _portraitsByID[customID];
                    }
                } else {
                    loaded = true;
                    return portraitData;
                }
            } catch (Exception e) {
                Mod.Log(e.ToString());
            }
            return null;
        }
        public static void OnPortraitGUI(string customID, float scaling = 0.5f, bool isButton = true, int targetWidth = 0) {
            PortraitData portraitData = LoadCustomPortrait(customID, out var loaded);
            if (loaded) {
                var sprite = portraitData.FullLengthPortrait;
                int w, h;
                if (targetWidth == 0) {
                    w = (int)(sprite.rect.width * scaling);
                    h = (int)(sprite.rect.height * scaling);
                } else {
                    w = targetWidth;
                    h = (int)(targetWidth * (sprite.rect.height / sprite.rect.width));
                }
                using (VerticalScope((w + 10).width())) {
                    if (isButton) {
                        if (GUILayout.Button(sprite.texture, rarityStyle, w.width(), h.height())) {
                            newPortraitName = customID;
                        }
                    } else {
                        GUILayout.Label(sprite.texture, rarityStyle, w.width(), h.height());
                    }
                    Label(customID);
                }
            }
        }
        public static void OnPortraitGUI(BlueprintPortrait portrait, float scaling = 0.5f, bool isButton = true, int targetWidth = 0) {
            if (portrait != null) {
                var sprite = portrait.FullLengthPortrait;
                if (sprite == null) return;
                int w, h;
                if (targetWidth == 0) {
                    w = (int)(sprite.rect.width * scaling);
                    h = (int)(sprite.rect.height * scaling);
                } else {
                    w = targetWidth;
                    h = (int)(targetWidth * (sprite.rect.height / sprite.rect.width));
                }
                using (VerticalScope((w + 10).width())) {
                    if (isButton) {
                        if (GUILayout.Button(sprite.texture, rarityStyle, w.width(), h.height())) {
                            newBlueprintPortrait = portrait;
                        }
                    } else {
                        GUILayout.Label(sprite.texture, rarityStyle, w.width(), h.height());
                    }
                    ActionButton("Save as png".localize(), () => {
                        try {
                            var portraitDir = new DirectoryInfo(Path.Combine(Main.path, "Portraits", portrait.name));
                            if (!portraitDir.Exists) {
                                portraitDir.Create();
                            }
                            var outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitSmallName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                            portrait.SmallPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                            outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitMediumName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                            portrait.HalfLengthPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                            outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitBigName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                            portrait.FullLengthPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, MiscExtensions.SaveTextureFileFormat.PNG, 100, false);
                            Process.Start(portraitDir.FullName);
                        } catch (Exception ex) {
                            Mod.Error(ex.ToString());
                        }
                    });
                    Label(BlueprintExtensions.GetTitle(portrait), MinWidth(200), AutoWidth());
                }
            }
        }
        public static List<Action> OnStatsGUI(BaseUnitEntity ch) {
            List<Action> todo = new();
            using (HorizontalScope()) {
                100.space();
                using (VerticalScope()) {
                    if (ch.UISettings.Portrait.IsCustom) {
                        Label("Current Custom Portrait".localize());
                        OnPortraitGUI(ch.UISettings.Portrait.CustomId, 0.25f, false);
                    } else {
                        Label("Current Blueprint Portrait".localize());
                        OnPortraitGUI(ch.UISettings.PortraitBlueprint, 0.25f, false, (int)(0.25f * 692));
                    }
                    using (HorizontalScope()) {
                        Label("Gender".localize(), Width(180));
                        Space(25);
                        var gender = ch.Descriptor().GetCustomGender() ?? ch.Descriptor().Gender;
                        var isFemale = gender == Gender.Female;
                        using (HorizontalScope(Width(100))) {
                            if (Toggle(isFemale ? "Female".localize() : "Male".localize(), ref isFemale,
                                RichText.Bold("♀".Color(RGBA.magenta)),
                                RichText.Bold("♂".Color(RGBA.aqua)),
                                0, largeStyle, GUI.skin.box, Width(200), Height(20))) {
                                ch.Descriptor().SetCustomGender(isFemale ? Gender.Female : Gender.Male);
                            }
                        }
                        Label(RichText.Green("Changing your gender may cause visual glitches".localize()));
                    }
                    Div(0, 20, 755);
                    DisclosureToggle("Show Custom Portrait Picker".localize(), ref listCustomPortraits);
                    if (listCustomPortraits) {
                        using (HorizontalScope()) {
                            Label("Name of the new Custom Portrait: ".localize(), Width(425));
                            TextField(ref newPortraitName, null, MinWidth(200), AutoWidth());
                            ActionButton("Change Portrait".localize(), () => todo.Add(() => {
                                if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds().Contains(newPortraitName)) {
                                    ch.UISettings.SetPortrait(new PortraitData(newPortraitName));
                                    Mod.Debug($"Changed portrait of {ch.CharacterName} to {newPortraitName}");
                                    unknownID = false;
                                } else {
                                    Mod.Warn($"No portrait with name {newPortraitName}");
                                    unknownID = true;
                                }
                            }));
                            if (unknownID) {
                                25.space();
                                Label(RichText.Red("Unknown ID!".localize()));
                            }
                        }
                        if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds() is string[] customIDs) {
                            if (portraitBrowser == null) {
                                portraitBrowser = new(true, true, false, true);
                                _portraitsLoaded = true;
                                portraitBrowser.SearchLimit = 18;
                                portraitBrowser.DisplayShowAllGUI = false;
                            }
                            portraitBrowser.OnGUI(customIDs, () => customIDs, ID => ID, ID => ID, ID => new[] { ID }, null, null, null, 0, true, true, 100, 300, "", false, null,
                                (definitions, _currentDict) => {
                                    var count = definitions.Count;
                                    using (VerticalScope()) {
                                        for (var ii = 0; ii < count;) {
                                            var tmp = ii;
                                            using (HorizontalScope()) {
                                                for (; ii < Math.Min(tmp + 6, count); ii++) {
                                                    var customID = definitions[ii];
                                                    // 6 Portraits per row; 692px per image + buffer
                                                    OnPortraitGUI(customID, (ummWidth - 100) / (6 * 780));
                                                }
                                            }
                                        }
                                    }
                                });
                        }
                    }
                    DisclosureToggle("Show Blueprint Portrait Picker".localize(), ref listBlueprintPortraits);
                    if (listBlueprintPortraits) {
                        using (HorizontalScope()) {
                            Label("Name of the new Blueprintportrait: ".localize(), Width(425));
                            if (newBlueprintPortrait != null)
                                Label(BlueprintExtensions.GetTitle(newBlueprintPortrait), MinWidth(200), AutoWidth());
                            else
                                200.space();
                            ActionButton("Change Portrait".localize(), () => todo.Add(() => {
                                if (newBlueprintPortrait != null) {
                                    ch.UISettings.SetPortrait(newBlueprintPortrait);
                                    Mod.Debug($"Changed portrait of {ch.CharacterName} to {BlueprintExtensions.GetTitle(newBlueprintPortrait)}");
                                }
                            }));
                        }
                        if (Event.current.type == EventType.Layout && (blueprintPortraitBps?.Count() ?? 0) == 0) {
                            blueprintPortraitBps = BlueprintLoader.Shared.GetBlueprintsOfType<BlueprintPortrait>();
                        }
                        if ((blueprintPortraitBps?.Count() ?? 0) > 0) {
                            if (blueprintPortraitBrowser == null) {
                                blueprintPortraitBrowser = new(true, true, false, true);
                                blueprintPortraitBrowser.SearchLimit = 18;
                                blueprintPortraitBrowser.DisplayShowAllGUI = false;
                            }
                            blueprintPortraitBrowser.OnGUI(blueprintPortraitBps, () => blueprintPortraitBps, ID => ID, ID => BlueprintExtensions.GetSearchKey(ID), ID => new[] { BlueprintExtensions.GetSortKey(ID) }, null, null, null, 0, true, true, 100, 300, "", false, null,
                                (definitions, _currentDict) => {
                                    var count = definitions.Count;
                                    using (VerticalScope()) {
                                        for (var ii = 0; ii < count;) {
                                            var tmp = ii;
                                            using (HorizontalScope()) {
                                                for (; ii < Math.Min(tmp + 6, count); ii++) {
                                                    // 6 Portraits per row; 692px per image + buffer
                                                    OnPortraitGUI(definitions[ii], 3.76f * ((ummWidth - 100) / (6 * 780)), true, (int)(692 * (ummWidth - 100) / (6 * 780)));
                                                }
                                            }
                                        }
                                    }
                                });
                        }
                    }
                    DisclosureToggle("Show Blueprint Voice Picker".localize(), ref listCustomVoices);
                    if (listCustomVoices) {
                        if (ch != null) {
                            if (ch.Asks.List != null) {
                                if (!(ch.IsCustomCompanion() || ch.IsMainCharacter)) {
                                    Label(RichText.Bold(RichText.Red("You're about to change the voice of a non-custom character. That's untested.".localize())));
                                } else if (!BlueprintExtensions.GetTitle(ch.Asks.List).StartsWith("RT")) {
                                    Label(RichText.Bold(RichText.Red("You have given a custom character a non-default voice. That's untested.".localize())));
                                }
                            }
                            if (blueprintVoiceBrowser?.ShowAll ?? false) {
                                Label(RichText.Bold(RichText.Red("Giving characters voices besides the default ones is untested.".localize())));
                            }
                            if (Event.current.type == EventType.Layout && (blueprintVoiceBps?.Count() ?? 0) == 0) {
                                blueprintVoiceBps = BlueprintLoader.Shared.GetBlueprintsOfType<BlueprintUnitAsksList>();
                            }
                            if ((blueprintVoiceBps?.Count() ?? 0) > 0) {
                                if (blueprintVoiceBrowser == null) {
                                    blueprintVoiceBrowser = new(true, true);
                                    blueprintVoiceBrowser.SearchLimit = 18;
                                }
                                blueprintVoiceBrowser.OnGUI(blueprintVoiceBps.Where(v => BlueprintExtensions.GetTitle(v).StartsWith("RT")).ToList(), () => blueprintVoiceBps, ID => ID, ID => BlueprintExtensions.GetSearchKey(ID), ID => new[] { BlueprintExtensions.GetSortKey(ID) }, null,
                                (definition, _currentDict) => {
                                    bool isCurrentVoice = definition == ch.Asks.List;
                                    if (isCurrentVoice) {
                                        Label(RichText.Green(BlueprintExtensions.GetTitle(definition)), 300.width());
                                        ActionButton("Play Example".localize(), () => {
                                            new BarkWrapper(definition.GetComponent<UnitAsksComponent>().PartyMemberUnconscious, ch.View.Asks).Schedule();
                                        }, 150.width());
                                    } else {
                                        Label(BlueprintExtensions.GetTitle(definition), 300.width());
                                        Space(150);
                                    }
                                    Space(200);
                                    if (isCurrentVoice) {
                                        Label("This is the current voice!".localize());
                                    } else {
                                        ActionButton("Change Voice".localize(), () => {
                                            if (definition != null) {
                                                todo.Add(() => {
                                                    ch.Asks.SetCustom(definition);
                                                    ch.View.UpdateAsks();
                                                });
                                                Mod.Debug($"Changed voice of {ch.CharacterName} to {BlueprintExtensions.GetTitle(definition)}");
                                            }
                                        });
                                    }
                                });
                            }
                        }
                    }
                    var cName = ch.Blueprint?.CharacterName?.ToLower() ?? ch.Blueprint.AssetGuid.ToString();
                    bool DisableVO = Settings.namesToDisableVoiceOver.Contains(cName);
                    if (Toggle("Disable Voice Over and Barks for this character".localize(), ref DisableVO, Width(425))) {
                        if (DisableVO) Settings.namesToDisableVoiceOver.Add(cName);
                        else Settings.namesToDisableVoiceOver.Remove(cName);
                    }
                    using (HorizontalScope()) {
                        if (!Main.Settings.perSave.doOverrideEnableAiForCompanions.TryGetValue(ch.HashKey(), out var valuePair)) {
                            valuePair = new(false, false);
                        }
                        var temp = valuePair.Item1;
                        if (Toggle("Override AI Control Behaviour".localize(), ref temp)) {
                            if (temp) {
                                Main.Settings.perSave.doOverrideEnableAiForCompanions[ch.HashKey()] = new(temp, valuePair.Item2);
                                Settings.SavePerSaveSettings();
                            } else {
                                Main.Settings.perSave.doOverrideEnableAiForCompanions.Remove(ch.HashKey());
                                Settings.SavePerSaveSettings();
                            }
                        }
                        if (temp) {
                            Space(50);
                            var temp2 = valuePair.Item2;
                            if (Toggle("Make Character AI Controlled".localize(), ref temp2)) {
                                Main.Settings.perSave.doOverrideEnableAiForCompanions[ch.HashKey()] = new(temp, temp2);
                                Settings.SavePerSaveSettings();
                            }
                        }
                    }
                }
            }
            Div(100, 20, 755);
            using (HorizontalScope()) {
                Space(100);

                using (VerticalScope()) {

                    DisclosureToggle("Body parts offsets and position".localize(), ref listSkeletonOffsets);
                    if (listSkeletonOffsets) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].groupOF.Keys) {

                                using (HorizontalScope()) {

                                    using (VerticalScope(Width(325))) {

                                        Label(key.localize().Color(RGBA.none), Width(325));
                                        Space(-6.point());
                                    }

                                    var bodyPart = skeletonReplacers[ch.HashKey()].groupOF[key];
                                    var min = bodyPart.min;
                                    var max = bodyPart.max;

                                    if (Slider(ref bodyPart.parameter, min, max, 0, 2, "", AutoWidth())) {

                                        skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false, key);
                                        Settings.SavePerSaveSettings();
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new SkeletonReplacer(ch);
                        }
                    }

                    DisclosureToggle("Body parts global scales".localize(), ref listSkeletonScales);
                    if (listSkeletonScales) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].groupSC.Keys) {

                                using (HorizontalScope()) {

                                    var bodyPart = skeletonReplacers[ch.HashKey()].groupSC[key];
                                    var min = bodyPart.min;
                                    var max = bodyPart.max;

                                    if (LogSliderCustomLabelWidth(key.localize().Color(RGBA.none), ref bodyPart.parameter, min, max, 1, 2, "", 300, AutoWidth())) {

                                        skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false, key);
                                        Settings.SavePerSaveSettings();
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new SkeletonReplacer(ch);
                        }
                    }

                    DisclosureToggle("Body parts local sizes".localize(), ref listSkeletonSizes);
                    if (listSkeletonSizes) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].groupSZ.Keys) {

                                using (HorizontalScope()) {

                                    var bodyPart = skeletonReplacers[ch.HashKey()].groupSZ[key];
                                    var min = bodyPart.min;
                                    var max = bodyPart.max;

                                    if (LogSliderCustomLabelWidth(key.localize().Color(RGBA.none), ref bodyPart.parameter, min, max, 1, 2, "", 300, AutoWidth())) {

                                        skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false, key);
                                        Settings.SavePerSaveSettings();
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new SkeletonReplacer(ch);
                        }
                    }

                    DisclosureToggle("Equipment elements sizes".localize(), ref listSkeletonItems);
                    if (listSkeletonItems) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].groupIT.Keys) {

                                using (HorizontalScope()) {

                                    var bodyPart = skeletonReplacers[ch.HashKey()].groupIT[key];
                                    var min = bodyPart.min;
                                    var max = bodyPart.max;

                                    if (LogSliderCustomLabelWidth(key.localize().Color(RGBA.none), ref bodyPart.parameter, min, max, 1, 2, "", 300, AutoWidth())) {

                                        skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false, key);
                                        Settings.SavePerSaveSettings();
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new SkeletonReplacer(ch);
                        }
                    }
                }
            }
            Div(100, 20, 755);
            if (ch != null && ch.HashKey() != null) {
                using (HorizontalScope()) {
                    Space(100);
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Label("Size".localize(), Width(325));
                            var size = ch.Descriptor().State.Size;
                            Label(RichText.Bold(RichText.Orange($"{size}")), Width(175));
                        }
                        Label("Pick size modifier to overwrite default.".localize());
                        Label("Pick none to stop overwriting.".localize());
                        using (HorizontalScope()) {
                            Space(328);
                            int tmp = 0;
                            if (Main.Settings.perSave.characterSizeModifier.TryGetValue(ch.HashKey(), out var tmpSize)) {
                                tmp = ((int)tmpSize) + 1;
                                // Applying again in case the game decided to change the modifier. Since this is an OnGUI it'll still only happen if the GUI is open though.
                                ch.Descriptor().State.Size = tmpSize;
                            }
                            var names = Enum.GetNames(typeof(Size)).Prepend("None").Select(name => name.localize()).ToArray();
                            ActionSelectionGrid(
                                ref tmp,
                                names,
                                3,
                               (s) => {
                                   // if == 0 then "None" is selected
                                   if (tmp > 0) {
                                       var newSize = (Size)(tmp - 1);
                                       ch.Descriptor().State.Size = newSize;
                                       Main.Settings.perSave.characterSizeModifier[ch.HashKey()] = newSize;
                                       Settings.SavePerSaveSettings();
                                   } else {
                                       Main.Settings.perSave.characterSizeModifier.Remove(ch.HashKey());
                                       Settings.SavePerSaveSettings();
                                       ch.Descriptor().State.Size = ch.Descriptor().OriginalSize;
                                   }
                               },
                                Width(420));
                        }
                    }
                }
                Space(20);
                using (HorizontalScope()) {
                    Space(100);
                    if (ch.View?.gameObject?.transform?.localScale[0] is float scaleMultiplier) {
                        var lastScale = lastScaleSize.GetValueOrDefault(ch.HashKey(), 1);
                        if (lastScale != scaleMultiplier) {
                            ch.View.gameObject.transform.localScale = new Vector3(lastScale, lastScale, lastScale);
                        }
                        if (LogSliderCustomLabelWidth("Visual Character Size Multiplier".localize().Color(RGBA.none), ref lastScale, 0.01f, 40f, 1, 2, "", 300, AutoWidth())) {
                            Main.Settings.perSave.characterModelSizeMultiplier[ch.HashKey()] = lastScale;
                            ch.View.gameObject.transform.localScale = new Vector3(lastScale, lastScale, lastScale);
                            lastScaleSize[ch.HashKey()] = lastScale;
                            Settings.SavePerSaveSettings();
                        }
                    }
                }
                Space(10);
                Div(100, 20, 755);
            }
            // TODO: Actually implement this for companions.
            if (ch.IsMainCharacter) {
                var soulMarks = ch.GetSoulMarks();
                using (HorizontalScope()) {
                    100.space();
                    Label("Soul Marks".localize(), Width(300));
                    using (VerticalScope()) {
                        foreach (SoulMarkDirection dir in Enum.GetValues(typeof(SoulMarkDirection))) {
                            if (dir == SoulMarkDirection.None || dir == SoulMarkDirection.Reason) continue;
                            SoulMark soulMark = null;
                            try {
                                soulMark = SoulMarkShiftExtension.GetSoulMarkFor(ch, dir);
                                if (soulMark == null) continue;
                                /*{        
                                var f = Shodan.MainCharacter.Blueprint.m_AddFacts.Select(f => f.Get()).OfType<BlueprintSoulMark>().Where(f => f == SoulMarkShiftExtension.GetBaseSoulMarkFor(dir)).First();
                                ch.AddFact(f);
                                soulMark = SoulMarkShiftExtension.GetSoulMarkFor(ch, dir);
                                }*/
                            } catch (Exception ex) {
                                Mod.Error(ex);
                                continue;
                            }
                            using (HorizontalScope()) {
                                Label(RichText.Bold(RichText.Orange(dir.ToString().localize())), 95.width());
                                ActionButton(" < ",
                                             () => modifySoulmark(dir, soulMark, ch, soulMark.Rank - 1, soulMark.Rank - 2),
                                             GUI.skin.box,
                                             AutoWidth());
                                Space(20);
                                var val = soulMark.Rank - 1;
                                Label(RichText.Bold(RichText.Orange($"{val}")), Width(50f));
                                ActionButton(" > ",
                                             () => modifySoulmark(dir, soulMark, ch, soulMark.Rank - 1, soulMark.Rank),
                                             GUI.skin.box,
                                             AutoWidth());
                                Space(25);
                                val = soulMark.Rank - 1;
                                ActionIntTextField(ref val, (v) => {
                                    if (v > 0) {
                                        modifySoulmark(dir, soulMark, ch, soulMark.Rank - 1, v);
                                    }
                                },
                                    Width(75));
                            }
                        }
                    }
                }
            }
            Div(100, 20, 755);
            foreach (var obj in HumanFriendlyStats.StatTypes) {
                try {
                    var statType = (StatType)obj;
                    Mod.Debug($"stat: {statType}");
                    var modifiableValue = ch.Stats.GetStatOptional(statType);
                    if (modifiableValue == null) {
                        continue;
                    }

                    var key = $"{ch.CharacterName}-{statType}";
                    var storedValue = statEditorStorage.ContainsKey(key) ? statEditorStorage[key] : modifiableValue.ModifiedValue;
                    var statName = statType.ToString();
                    if (statName == "BaseAttackBonus" || statName == "SkillAthletics" || statName == "HitPoints") {
                        Div(100, 20, 755);
                    }
                    using (HorizontalScope()) {
                        Space(100);
                        Label(statName.localize(), Width(374f));
                        Space(25);
                        ActionButton(" < ",
                                     () => {
                                         modifiableValue.BaseValue -= 1;
                                         storedValue = modifiableValue.ModifiedValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(20);
                        var val = modifiableValue.ModifiedValue;
                        Label(RichText.Bold(RichText.Orange($"{val}")), Width(50f));
                        ActionButton(" > ",
                                     () => {
                                         modifiableValue.BaseValue += 1;
                                         storedValue = modifiableValue.ModifiedValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(25);
                        ActionIntTextField(ref storedValue, (v) => {

                            modifiableValue.BaseValue += v - modifiableValue.ModifiedValue;
                            storedValue = modifiableValue.ModifiedValue;
                        }, Width(75));
                        statEditorStorage[key] = storedValue;
                    }
                } catch (Exception ex) {
                    Mod.Trace(ex.ToString());
                }
            }
            return todo;
        }

        private static void modifySoulmark(SoulMarkDirection dir, SoulMark soulMark, BaseUnitEntity ch, int oldRank, int v) {
            var change = v - oldRank;
            if (change > 0) {
                var soulMarkShift = new SoulMarkShift() { CheckByRank = false, Direction = dir, Value = change };
                new BlueprintAnswer() { SoulMarkShift = soulMarkShift }.ApplyShiftDialog();
            } else if (change < 0) {
                var soulMarkShift = new SoulMarkShift() { CheckByRank = false, Direction = dir, Value = change };
                var provider = new BlueprintAnswer() { SoulMarkShift = soulMarkShift };
                var source = provider as BlueprintScriptableObject;
                if (source != null) {
                    EntityFactSource entityFactSource = new EntityFactSource(source, new int?(change));
                    if (!soulMark.Sources.ToList().HasItem(entityFactSource)) {
                        soulMark.AddSource(source, change);
                        soulMark.RemoveRank(-change);
                    }
                }
                Game.Instance.DialogController.SoulMarkShifts.Add(provider.SoulMarkShift);
                EventBus.RaiseEvent<ISoulMarkShiftHandler>(delegate (ISoulMarkShiftHandler h) {
                    h.HandleSoulMarkShift(provider);
                }, true);
            }
        }
    }
}
