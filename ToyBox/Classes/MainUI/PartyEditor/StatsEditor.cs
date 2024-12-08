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

namespace ToyBox {

    public partial class PartyEditor {

        public class ToyBoxAlignmentProvider : IAlignmentShiftProvider {

            AlignmentShift IAlignmentShiftProvider.AlignmentShift => new() { Description = "ToyBox Party Editor".LocalizedStringInGame() };
        }

        public class CharacterSkeletonReplacer {

            public Skeleton oldSkeleton;
            public Skeleton newSkeleton;
            public Dictionary<string, BodyPart> bodyParts;
            public Dictionary<string, BodyPart> groupOF;
            public Dictionary<string, BodyPart> groupSC;
            public Dictionary<string, BodyPart> groupSZ;
            public Dictionary<string, BodyPart> groupIT;

            public CharacterSkeletonReplacer(BaseUnitEntity character) {

                if (character.View?.CharacterAvatar?.Skeleton is Skeleton skeleton) {

                    oldSkeleton = skeleton;
                    newSkeleton = DuplicateSkeleton(skeleton);
                    bodyParts = new Dictionary<string, BodyPart>();
                    groupOF = new Dictionary<string, BodyPart>();
                    groupSC = new Dictionary<string, BodyPart>();
                    groupSZ = new Dictionary<string, BodyPart>();
                    groupIT = new Dictionary<string, BodyPart>();

                    var partsTable = new Dictionary<string, List<string>> {

                        { "OF_position",
                            new List<string> { "Position" }},
                        { "OF_shouldersX",
                            new List<string> { "R_Clavicle", "L_Clavicle" }},
                        { "OF_shouldersZ",
                            new List<string> { "R_Clavicle", "L_Clavicle" }},
                        { "OF_upper_arms",
                            new List<string> { "R_Up_arm", "L_Up_arm" }},
                        { "OF_fore_arms",
                            new List<string> { "R_ForeArm", "L_ForeArm" }},
                        { "OF_upper_legs",
                            new List<string> { "R_Pre_Up_Leg", "L_Pre_Up_Leg" }},
                        { "SC_pelvisX",
                            new List<string> { "Pelvis" }},
                        { "SC_pelvisY",
                            new List<string> { "Pelvis" }},
                        { "SC_pelvisZ",
                            new List<string> { "Pelvis" }},
                        { "SC_neck",
                            new List<string> { "Neck" }},
                        { "SC_shoulders",
                            new List<string> { "R_Clavicle", "L_Clavicle" }},
                        { "SC_upper_arms",
                            new List<string> { "R_Up_arm", "L_Up_arm" }},
                        { "SC_fore_arms",
                            new List<string> { "R_ForeArm", "L_ForeArm" }},
                        { "SC_upper_torso",
                            new List<string> { "Spine_3" }},
                        { "SC_middle_torso",
                            new List<string> { "Spine_2" }},
                        { "SC_lower_torso",
                            new List<string> { "Spine_1" }},
                        { "SC_stomach",
                            new List<string> { "Stomach" }},
                        { "SC_upper_legs",
                            new List<string> { "R_Pre_Up_Leg", "L_Pre_Up_Leg" }},
                        { "SC_lower_legs",
                            new List<string> { "R_Up_leg", "L_Up_leg" }},
                        { "SC_foots",
                            new List<string> { "R_foot", "L_foot" }},
                        { "SC_toes",
                            new List<string> { "R_toe", "L_toe" }},
                        { "SZ_head",
                            new List<string> { "Head" }},
                        { "SZ_neck",
                            new List<string> { "Neck_ADJ" }},
                        { "SZ_shoulders",
                            new List<string> { "R_Clavicle_ADJ", "L_Clavicle_ADJ" }},
                        { "SZ_upper_arms",
                            new List<string> { "R_Up_arm_ADJ", "L_Up_arm_ADJ" }},
                        { "SZ_fore_arms",
                            new List<string> { "R_ForeArm_ADJ", "R_ForeArm_Twist_ADJ", "L_ForeArm_ADJ", "L_ForeArm_Twist_ADJ" }},
                        { "SZ_hands",
                            new List<string> { "R_Hand", "L_Hand" }},
                        { "SZ_upper_torso",
                            new List<string> { "Spine_3_ADJ" }},
                        { "SZ_middle_torso",
                            new List<string> { "Spine_2_ADJ" }},
                        { "SZ_lower_torso",
                            new List<string> { "Spine_1_ADJ" }},
                        { "SZ_stomach",
                            new List<string> { "Stomach_ADJ" }},
                        { "SZ_pelvis",
                            new List<string> { "Pelvis_ADJ" }},
                        { "SZ_upper_legs",
                            new List<string> { "R_Up_leg_ADJ", "L_Up_leg_ADJ" }},
                        { "SZ_middle_legs",
                            new List<string> { "R_leg_ADJ", "L_leg_ADJ" }},
                        { "SZ_lower_legs",
                            new List<string> { "R_Ankle_ADJ", "L_Ankle_ADJ" }},
                        { "SZ_foots",
                            new List<string> { "R_foot_ADJ", "L_foot_ADJ" }},
                        { "SZ_toes",
                            new List<string> { "R_toe_ADJ", "L_toe_ADJ" }},
                        { "IT_backpack_and_cloak",
                            new List<string> { "C_back_weapon_slot_08", "C_back_weapon_slot_11" }},
                        { "IT_weapon_in_hand",
                            new List<string> { "R_WeaponBone", "L_WeaponBone" }},
                        { "IT_weapon_in_holsters",
                            new List<string> { "R_front_weapon_slot_01", "R_front_weapon_slot_02", "C_front_weapon_slot_03", "L_front_weapon_slot_04", "L_front_weapon_slot_05" }},
                        { "IT_back_weapon_right",
                            new List<string> { "R_back_weapon_slot_06", "R_back_weapon_slot_09" }},
                        { "IT_back_weapon_left",
                            new List<string> { "L_back_weapon_slot_07", "L_back_weapon_slot_10" }}
                    };

                    CreateBodyParts(partsTable, bodyParts);
                }
            }

            public void CreateBodyParts(Dictionary<string, List<string>> bodyPartsTable, Dictionary<string, BodyPart> bodyParts) {

                foreach (string key in bodyPartsTable.Keys) {

                    if (key.StartsWith("OF_")) {

                        groupOF[key] = new BodyPart(0);
                        bodyParts[key] = groupOF[key];

                    } else if (key.StartsWith("SC_")) {

                        groupSC[key] = new BodyPart(1);
                        bodyParts[key] = groupSC[key];

                    } else if (key.StartsWith("SZ_")) {

                        groupSZ[key] = new BodyPart(1);
                        bodyParts[key] = groupSZ[key];

                    } else if (key.StartsWith("IT_")) {

                        groupIT[key] = new BodyPart(1);
                        bodyParts[key] = groupIT[key];

                    } else {

                        bodyParts[key] = new BodyPart(1);
                    }

                    var isOffsetPart = groupOF.ContainsKey(key);

                    foreach (string value in bodyPartsTable[key]) {

                        if (oldSkeleton.BonesByName.ContainsKey(value)) {

                            bodyParts[key].isEmpty = false;

                            var name = value;
                            var index = oldSkeleton.Bones.IndexOf(oldSkeleton.BonesByName[name]);
                            var offset = oldSkeleton.Bones[index].ApplyOffset = isOffsetPart;
                            var oldValue = isOffsetPart ? oldSkeleton.Bones[index].Offset : oldSkeleton.Bones[index].Scale;
                            var boneData = new BoneDataStruct { boneName = name, boneIndex = index, applyOffset = offset, originalValue = oldValue };

                            bodyParts[key].bonesData.Add(boneData);
                        }
                    }
                }
            }

            public Skeleton DuplicateSkeleton(Skeleton skeleton) {

                var tempSkeleton = new Skeleton();
                var newBoneJobArray = new NativeArray<Skeleton.BoneData>(skeleton.Bones.Count, Allocator.Persistent);

                for (int i = 0; i < skeleton.Bones.Count; i++) {

                    newBoneJobArray[i] = new Skeleton.BoneData { ApplyOffset = skeleton.Bones[i].ApplyOffset, Offset = skeleton.Bones[i].Offset, Scale = skeleton.Bones[i].Scale };
                }

                tempSkeleton.m_BoneDataForJob = newBoneJobArray;
                tempSkeleton.name = skeleton.name;
                tempSkeleton.Bones = skeleton.Bones;
                tempSkeleton.hideFlags = skeleton.hideFlags;
                tempSkeleton.AnimationSetOverride = skeleton.AnimationSetOverride;
                tempSkeleton.CharacterFxBonesMap = skeleton.CharacterFxBonesMap;
                tempSkeleton.RaceBoneHierarchyObject = skeleton.RaceBoneHierarchyObject;
                tempSkeleton.m_DollRoomZoomPreset = skeleton.m_DollRoomZoomPreset;
                tempSkeleton.m_IsDirty = skeleton.m_IsDirty;
                tempSkeleton.m_BonesByName = skeleton.m_BonesByName;
                tempSkeleton.m_IsDirty = skeleton.m_IsDirty;

                return tempSkeleton;
            }

            public void ApplyBonesModification(BaseUnitEntity character, bool loadPerSaveData) {

                if (character?.View?.CharacterAvatar?.Skeleton != newSkeleton) {

                    character.View.CharacterAvatar.Skeleton = newSkeleton;
                }

                var specificBodyParts = new Dictionary<string, BodyPart>();

                foreach (string key in bodyParts.Keys) {

                    foreach (BoneDataStruct bone in bodyParts[key].bonesData) {

                        var tarrgetBone = newSkeleton.m_BoneDataForJob[bone.boneIndex];
                        var parameter = bodyParts[key].parameter;

                        if (Main.Settings.perSave.characterSkeletonReplacers.ContainsKey(character.HashKey()) && loadPerSaveData) {

                            var boneData = Main.Settings.perSave.characterSkeletonReplacers[character.HashKey()];
                            parameter = boneData.GetValueOrDefault(key, 1);

                        } else {

                            if (!Main.Settings.perSave.characterSkeletonReplacers.ContainsKey(character.HashKey())) {

                                Main.Settings.perSave.characterSkeletonReplacers[character.HashKey()] = new Dictionary<string, float>();
                            }

                            Main.Settings.perSave.characterSkeletonReplacers[character.HashKey()][key] = parameter;
                        }

                        bodyParts[key].parameter = parameter;

                        if (groupOF.ContainsKey(key) || groupIT.ContainsKey(key) || key.StartsWith("SC_pelvis")) {

                            specificBodyParts[key] = bodyParts[key];

                        } else {

                            tarrgetBone.Scale = bone.originalValue * parameter;
                        }

                        newSkeleton.m_BoneDataForJob[bone.boneIndex] = tarrgetBone;
                    }
                }

                var m1 = bodyParts.ContainsKey("SC_lower_torso") ? bodyParts["SC_lower_torso"].parameter : 1;
                var m2 = bodyParts.ContainsKey("SC_middle_torso") ? bodyParts["SC_middle_torso"].parameter : 1;
                var m3 = bodyParts.ContainsKey("SC_upper_torso") ? bodyParts["SC_upper_torso"].parameter : 1;
                var m4 = bodyParts.ContainsKey("SC_shoulders") ? bodyParts["SC_shoulders"].parameter : 1;
                var m5 = bodyParts.ContainsKey("SC_upper_arms") ? bodyParts["SC_upper_arms"].parameter : 1;
                var m6 = bodyParts.ContainsKey("SC_fore_arms") ? bodyParts["SC_fore_arms"].parameter : 1;
                var m7 = bodyParts.ContainsKey("SZ_hands") ? bodyParts["SZ_hands"].parameter : 1;

                var bM = m1 * m2 * m3;
                var nM = bM * m4 * m5 * m6 * m7;

                foreach (string key in specificBodyParts.Keys) {

                    foreach (BoneDataStruct bone in specificBodyParts[key].bonesData) {

                        var tarrgetBone = newSkeleton.m_BoneDataForJob[bone.boneIndex];
                        var parameter = specificBodyParts[key].parameter;
                        var isRight = bone.boneName.StartsWith("R_");

                        tarrgetBone.ApplyOffset = bone.applyOffset;

                        if (key == "OF_position") {

                            tarrgetBone.Offset.x = bone.originalValue.x + parameter * -0.1f;

                        } else if (key.Contains("shouldersX")) {

                            tarrgetBone.Offset.z = bone.originalValue.z + (isRight ? parameter * 0.1f : parameter * -0.1f);

                        } else if (key.Contains("shouldersZ")) {

                            tarrgetBone.Offset.x = bone.originalValue.x + (isRight ? parameter * -0.1f : parameter * -0.1f);

                        } else if (key.Contains("upper_arms")) {

                            tarrgetBone.Offset.x = bone.originalValue.x + (isRight ? parameter * -0.1f : parameter * 0.1f);

                        } else if (key.Contains("fore_arms")) {

                            tarrgetBone.Offset.z = bone.originalValue.z + (isRight ? parameter * -0.1f : parameter * 0.1f);

                        } else if (key.Contains("upper_legs")) {

                            tarrgetBone.Offset.x = bone.originalValue.x + (isRight ? parameter * 0.1f : parameter * -0.1f);

                        } else if (key == "SC_pelvisX") {

                            tarrgetBone.Scale.x = bone.originalValue.x * parameter;

                        } else if (key == "SC_pelvisY") {

                            tarrgetBone.Scale.y = bone.originalValue.y * parameter;

                        } else if (key == "SC_pelvisZ") {

                            tarrgetBone.Scale.z = bone.originalValue.z * parameter;

                        } else if (key == "IT_weapon_in_holsters") {

                            tarrgetBone.Scale = bone.originalValue * parameter;

                        } else if (key == "IT_weapon_in_hand") {

                            tarrgetBone.Scale = (bone.originalValue * parameter) / nM;

                        } else if (key == "IT_backpack_and_cloak" || key == "IT_back_weapon_right" || key == "IT_back_weapon_left") {

                            tarrgetBone.Scale = (bone.originalValue * parameter) / bM;
                        }

                        newSkeleton.m_BoneDataForJob[bone.boneIndex] = tarrgetBone;
                    }
                }
            }

            public class BodyPart {

                public bool isEmpty = true;
                public float parameter;
                public List<BoneDataStruct> bonesData;

                public BodyPart(float genericParameter) {

                    parameter = genericParameter;
                    bonesData = new List<BoneDataStruct>();
                }
            }

            public struct BoneDataStruct {

                public string boneName;
                public int boneIndex;
                public bool applyOffset;
                public Vector3 originalValue;
            }
        }

        public static IAlignmentShiftProvider ToyboxAlignmentProvider => new ToyBoxAlignmentProvider();

        public static Dictionary<string, CharacterSkeletonReplacer> skeletonReplacers = new();
        public static Dictionary<string, Vector3> lastScaleSize = new();
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
        public static List<System.Action> OnStatsGUI(BaseUnitEntity ch) {
            List<System.Action> todo = new();
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
                                                for (; ii < System.Math.Min(tmp + 6, count); ii++) {
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
                                                for (; ii < System.Math.Min(tmp + 6, count); ii++) {
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
                                        Label(RichText.Green(BlueprintExtensions.GetTitle(definition)), 500.width());
                                        ActionButton("Play Example".localize(), () => {
                                            new BarkWrapper(definition.GetComponent<UnitAsksComponent>().PartyMemberUnconscious, ch.View.Asks).Schedule();
                                        }, 150.width());
                                    } else {
                                        Label(BlueprintExtensions.GetTitle(definition), 500.width());
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

                            foreach (string key in skeletonReplacers[ch.HashKey()].bodyParts.Keys) {

                                var min = key == "OF_position" ? -10f : -1.0f;
                                var max = key == "OF_position" ? 10f : 1f;

                                if (skeletonReplacers[ch.HashKey()].groupOF.ContainsKey(key)) {

                                    using (HorizontalScope()) {

                                        using (VerticalScope(Width(325))) {

                                            Label(key.localize().Color(RGBA.none), Width(325));
                                            Space(-6.point());
                                        }

                                        if (Slider(ref skeletonReplacers[ch.HashKey()].bodyParts[key].parameter, min, max, 0, 2, "", AutoWidth())) {

                                            skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false);
                                            Settings.SavePerSaveSettings();
                                        }
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new CharacterSkeletonReplacer(ch);
                        }
                    }
                    DisclosureToggle("Body parts global scales".localize(), ref listSkeletonScales);
                    if (listSkeletonScales) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].bodyParts.Keys) {

                                var min = key == "SC_stomach" ? 0.2f : 0.5f;
                                var max = key == "SC_stomach" ? 5f : 2f;

                                if (skeletonReplacers[ch.HashKey()].groupSC.ContainsKey(key)) {

                                    using (HorizontalScope()) {

                                        if (LogSliderCustomLabelWidth(key.localize().Color(RGBA.none), ref skeletonReplacers[ch.HashKey()].bodyParts[key].parameter, min, max, 1, 2, "", 300, AutoWidth())) {

                                            skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false);
                                            Settings.SavePerSaveSettings();
                                        }
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new CharacterSkeletonReplacer(ch);
                        }
                    }
                    DisclosureToggle("Body parts local sizes".localize(), ref listSkeletonSizes);
                    if (listSkeletonSizes) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].bodyParts.Keys) {

                                var min = key == "SZ_stomach" ? 0.2f : 0.5f;
                                var max = key == "SZ_stomach" ? 5f : 2f;

                                if (skeletonReplacers[ch.HashKey()].groupSZ.ContainsKey(key)) {

                                    using (HorizontalScope()) {

                                        if (LogSliderCustomLabelWidth(key.localize().Color(RGBA.none), ref skeletonReplacers[ch.HashKey()].bodyParts[key].parameter, min, max, 1, 2, "", 300, AutoWidth())) {

                                            skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false);
                                            Settings.SavePerSaveSettings();
                                        }
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new CharacterSkeletonReplacer(ch);
                        }
                    }
                    DisclosureToggle("Equipment elements sizes".localize(), ref listSkeletonItems);
                    if (listSkeletonItems) {
                        Space(6);
                        if (skeletonReplacers.ContainsKey(ch.HashKey())) {

                            foreach (string key in skeletonReplacers[ch.HashKey()].bodyParts.Keys) {

                                var min = 0.5f;
                                var max = 2f;

                                if (skeletonReplacers[ch.HashKey()].groupIT.ContainsKey(key)) {

                                    using (HorizontalScope()) {

                                        if (LogSliderCustomLabelWidth(key.localize().Color(RGBA.none), ref skeletonReplacers[ch.HashKey()].bodyParts[key].parameter, min, max, 1, 2, "", 300, AutoWidth())) {

                                            skeletonReplacers[ch.HashKey()].ApplyBonesModification(ch, false);
                                            Settings.SavePerSaveSettings();
                                        }
                                    }
                                }
                            }
                            Space(10);

                        } else {

                            skeletonReplacers[ch.HashKey()] = new CharacterSkeletonReplacer(ch);
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
                    if (ch.View?.gameObject?.transform?.localScale is Vector3 scaleMultiplier) {
                        var lastScale = lastScaleSize.GetValueOrDefault(ch.HashKey(), new Vector3(1, 1, 1));
                        if (lastScale.x != scaleMultiplier.x || lastScale.y != scaleMultiplier.y || lastScale.z != scaleMultiplier.z) {
                            ch.View.gameObject.transform.localScale = lastScale;
                        }
                        if (LogSliderCustomLabelWidth("Visual character size multiplier X".localize().Color(RGBA.none), ref lastScale.x, 0.01f, 10f, 1, 2, "", 300, AutoWidth())) {
                            Main.Settings.perSave.characterModelSizeMultiplier[ch.HashKey()] = new(lastScale.x, lastScale.y, lastScale.z);
                            ch.View.gameObject.transform.localScale = lastScale;
                            lastScaleSize[ch.HashKey()] = lastScale;
                            Settings.SavePerSaveSettings();
                        }
                    }
                }
                using (HorizontalScope()) {
                    Space(100);
                    if (ch.View?.gameObject?.transform?.localScale is Vector3 scaleMultiplier) {
                        var lastScale = lastScaleSize.GetValueOrDefault(ch.HashKey(), new Vector3(1, 1, 1));
                        if (lastScale.x != scaleMultiplier.x || lastScale.y != scaleMultiplier.y || lastScale.z != scaleMultiplier.z) {
                            ch.View.gameObject.transform.localScale = lastScale;
                        }
                        if (LogSliderCustomLabelWidth("Visual character size multiplier Y".localize().Color(RGBA.none), ref lastScale.y, 0.01f, 10f, 1, 2, "", 300, AutoWidth())) {
                            Main.Settings.perSave.characterModelSizeMultiplier[ch.HashKey()] = new(lastScale.x, lastScale.y, lastScale.z);
                            ch.View.gameObject.transform.localScale = lastScale;
                            lastScaleSize[ch.HashKey()] = lastScale;
                            Settings.SavePerSaveSettings();
                        }
                    }
                }
                using (HorizontalScope()) {
                    Space(100);
                    if (ch.View?.gameObject?.transform?.localScale is Vector3 scaleMultiplier) {
                        var lastScale = lastScaleSize.GetValueOrDefault(ch.HashKey(), new Vector3(1, 1, 1));
                        if (lastScale.x != scaleMultiplier.x || lastScale.y != scaleMultiplier.y || lastScale.z != scaleMultiplier.z) {
                            ch.View.gameObject.transform.localScale = lastScale;
                        }
                        if (LogSliderCustomLabelWidth("Visual character size multiplier Z".localize().Color(RGBA.none), ref lastScale.z, 0.01f, 10f, 1, 2, "", 300, AutoWidth())) {
                            Main.Settings.perSave.characterModelSizeMultiplier[ch.HashKey()] = new(lastScale.x, lastScale.y, lastScale.z);
                            ch.View.gameObject.transform.localScale = lastScale;
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
                    Label("Soul Marks".localize(), Width(200));
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
                                Label(RichText.Orange(dir.ToString().localize()), 200.width());
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
                        Label(statName.localize(), Width(400f));
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
