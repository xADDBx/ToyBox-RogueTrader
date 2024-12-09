using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.CharacterSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace ToyBox {

    public class SkeletonReplacer {

        public string owner;
        public Skeleton oldSkeleton;
        public Skeleton newSkeleton;
        public Dictionary<string, BodyPart> bodyParts;
        public Dictionary<string, BodyPart> groupOF;
        public Dictionary<string, BodyPart> groupSC;
        public Dictionary<string, BodyPart> groupSZ;
        public Dictionary<string, BodyPart> groupIT;

        public SkeletonReplacer(BaseUnitEntity character) {

            if (character?.View?.CharacterAvatar?.Skeleton is Skeleton skeleton) {

                owner = character.HashKey();

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
                    { "OF_backpack_and_cloakX",
                        new List<string> { "C_back_w_____slot_08", "C_back_w_____slot_11" }},
                    { "OF_backpack_and_cloakZ",
                        new List<string> { "C_back_w_____slot_08", "C_back_w_____slot_11" }},
                    { "OF_shouldersX",
                        new List<string> { "R_Clavicle", "L_Clavicle" }},
                    { "OF_shouldersZ",
                        new List<string> { "R_Clavicle", "L_Clavicle" }},
                    { "OF_upper_arms",
                        new List<string> { "R_Up_arm", "L_Up_arm" }},
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

        private void CreateBodyParts(Dictionary<string, List<string>> bodyPartsTable, Dictionary<string, BodyPart> bodyParts) {

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

        private Skeleton DuplicateSkeleton(Skeleton skeleton) {

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

            if (character?.HashKey() == owner && character?.View?.CharacterAvatar?.Skeleton is Skeleton skeleton) {

                character.View.CharacterAvatar.Skeleton = newSkeleton;

            } else {

                return;
            }

            if (!Main.Settings.perSave.characterSkeletonReplacers.ContainsKey(character.HashKey())) {

                Main.Settings.perSave.characterSkeletonReplacers[character.HashKey()] = new Dictionary<string, float>();
            }

            var loadedPartsData = Main.Settings.perSave.characterSkeletonReplacers[character.HashKey()];
            var specificBodyParts = new Dictionary<string, BodyPart>();

            foreach (string key in bodyParts.Keys) {

                float parameter;

                if (loadPerSaveData && loadedPartsData.ContainsKey(key)) {

                    parameter = loadedPartsData[key];
                    bodyParts[key].parameter = parameter;

                } else {

                    parameter = bodyParts[key].parameter;
                    loadedPartsData[key] = parameter;
                }

                if (groupOF.ContainsKey(key) || groupIT.ContainsKey(key) || key.StartsWith("SC_pelvis")) {

                    specificBodyParts[key] = bodyParts[key];
                }

                foreach (BoneDataStruct bone in bodyParts[key].bonesData) {

                    var tarrgetBone = newSkeleton.m_BoneDataForJob[bone.boneIndex];

                    if (!specificBodyParts.ContainsKey(key)) {

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

                    switch (key) {

                        case "OF_position":

                            tarrgetBone.Offset.x = bone.originalValue.x + parameter * -0.1f;
                        break;

                        case "OF_backpack_and_cloakX":

                            tarrgetBone.Offset.y = bone.originalValue.y + parameter * -0.1f;
                        break;

                        case "OF_backpack_and_cloakZ":

                            tarrgetBone.Offset.x = bone.originalValue.x + parameter * -0.1f;
                        break;

                        case var s when s.Contains("shouldersX"):

                            tarrgetBone.Offset.z = bone.originalValue.z + (isRight ? parameter * 0.1f : parameter * -0.1f);
                        break;

                        case var s when s.Contains("shouldersZ"):

                            tarrgetBone.Offset.x = bone.originalValue.x + (isRight ? parameter * -0.1f : parameter * -0.1f);
                        break;

                        case var s when s.Contains("upper_arms"):

                            tarrgetBone.Offset.x = bone.originalValue.x + (isRight ? parameter * -0.1f : parameter * 0.1f);
                        break;

                        case var s when s.Contains("upper_legs"):

                            tarrgetBone.Offset.x = bone.originalValue.x + (isRight ? parameter * 0.1f : parameter * -0.1f);
                        break;

                        case "SC_pelvisX":

                            tarrgetBone.Scale.x = bone.originalValue.x * parameter;
                        break;

                        case "SC_pelvisY":

                            tarrgetBone.Scale.y = bone.originalValue.y * parameter;
                        break;

                        case "SC_pelvisZ":

                            tarrgetBone.Scale.z = bone.originalValue.z * parameter;
                        break;

                        case "IT_weapon_in_holsters":

                            tarrgetBone.Scale = bone.originalValue * parameter;
                        break;

                        case "IT_weapon_in_hand":

                            tarrgetBone.Scale = (bone.originalValue * parameter) / nM;
                        break;

                        case var s when s == "IT_backpack_and_cloak" || s == "IT_back_weapon_right" || s == "IT_back_weapon_left":

                            tarrgetBone.Scale = (bone.originalValue * parameter) / bM;
                        break;

                        default:
                        break;
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
}
