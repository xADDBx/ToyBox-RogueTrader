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

        private string owner;
        private Skeleton oldSkeleton;
        private Skeleton newSkeleton;

        public Dictionary<string, BodyPart> bodyParts;
        public Dictionary<string, BodyPart> groupOF;
        public Dictionary<string, BodyPart> groupSC;
        public Dictionary<string, BodyPart> groupSZ;
        public Dictionary<string, BodyPart> groupIO;
        public Dictionary<string, BodyPart> groupIS;

        private Dictionary<string, Func<Skeleton.BoneData, BoneDataStruct, float, Skeleton.BoneData>> boneActions;

        public SkeletonReplacer(BaseUnitEntity character) {

            if (character?.View?.CharacterAvatar?.Skeleton is Skeleton skeleton) {

                owner = character.HashKey();

                oldSkeleton = skeleton;
                newSkeleton = DuplicateSkeleton(skeleton);
                bodyParts = new Dictionary<string, BodyPart>();
                groupOF = new Dictionary<string, BodyPart>();
                groupSC = new Dictionary<string, BodyPart>();
                groupSZ = new Dictionary<string, BodyPart>();
                groupIO = new Dictionary<string, BodyPart>();
                groupIS = new Dictionary<string, BodyPart>();

                var partsTable = new Dictionary<string, PartDataStruct> {

                    { "OF_positionZ",
                        new PartDataStruct{ value = 0, min = -10, max = 10, bones = new List<string>{"Position"}}},

                    { "OF_shouldersX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_Clavicle", "L_Clavicle"}}},

                    { "OF_shouldersZ",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_Clavicle", "L_Clavicle"}}},

                    { "OF_upper_armsX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_Up_arm", "L_Up_arm"}}},

                    { "OF_upper_legsX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_Pre_Up_Leg", "L_Pre_Up_Leg"}}},

                    { "SC_pelvisX",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Pelvis"}}},

                    { "SC_pelvisY",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Pelvis"}}},

                    { "SC_pelvisZ",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Pelvis"}}},

                    { "SC_neck",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Neck"}}},

                    { "SC_shoulders",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Clavicle", "L_Clavicle"}}},

                    { "SC_upper_arms",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Up_arm", "L_Up_arm"}}},

                    { "SC_fore_arms",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_ForeArm", "L_ForeArm"}}},

                    { "SC_upper_torso",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Spine_3"}}},

                    { "SC_middle_torso",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Spine_2"}}},

                    { "SC_lower_torso",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Spine_1"}}},

                    { "SC_stomach",
                        new PartDataStruct{ value = 1, min = 0.2f, max = 5, bones = new List<string>{"Stomach"}}},

                    { "SC_upper_legs",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Pre_Up_Leg", "L_Pre_Up_Leg"}}},

                    { "SC_lower_legs",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Up_leg", "L_Up_leg"}}},

                    { "SC_foots",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_foot", "L_foot"}}},

                    { "SC_toes",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_toe", "L_toe"}}},

                    { "SZ_head",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Head"}}},

                    { "SZ_neck",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Neck_ADJ"}}},

                    { "SZ_shoulders",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Clavicle_ADJ", "L_Clavicle_ADJ"}}},

                    { "SZ_upper_arms",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Up_arm_ADJ", "L_Up_arm_ADJ"}}},

                    { "SZ_fore_arms",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_ForeArm_ADJ", "R_ForeArm_Twist_ADJ", "L_ForeArm_ADJ", "L_ForeArm_Twist_ADJ"}}},

                    { "SZ_hands",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Hand", "L_Hand"}}},

                    { "SZ_upper_torso",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Spine_3_ADJ"}}},

                    { "SZ_middle_torso",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Spine_2_ADJ"}}},

                    { "SZ_lower_torso",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Spine_1_ADJ"}}},

                    { "SZ_stomach",
                        new PartDataStruct{ value = 1, min = 0.2f, max = 5, bones = new List<string>{"Stomach_ADJ"}}},

                    { "SZ_pelvis",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"Pelvis_ADJ"}}},

                    { "SZ_upper_legs",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Up_leg_ADJ", "L_Up_leg_ADJ"}}},

                    { "SZ_middle_legs",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_leg_ADJ", "L_leg_ADJ"}}},

                    { "SZ_lower_legs",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_Ankle_ADJ", "L_Ankle_ADJ"}}},

                    { "SZ_foots",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_foot_ADJ", "L_foot_ADJ"}}},

                    { "SZ_toes",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_toe_ADJ", "L_toe_ADJ"}}},

                    { "IO_cloakX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"C_back_weapon_slot_08_ADJ"}}},

                    { "IO_cloakY",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"C_back_weapon_slot_08_ADJ"}}},

                    { "IO_cloakZ",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"C_back_weapon_slot_08_ADJ"}}},

                    { "IO_backpackX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"C_back_w_____slot_08"}}},

                    { "IO_backpackY",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"C_back_w_____slot_08"}}},

                    { "IO_backpackZ",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"C_back_w_____slot_08"}}},

                    { "IO_weapon_in_holstersRX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_front_weapon_slot_01_ADJ", "R_front_weapon_slot_02_ADJ"}}},

                    { "IO_weapon_in_holstersRY",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_front_weapon_slot_01_ADJ", "R_front_weapon_slot_02_ADJ"}}},

                    { "IO_weapon_in_holstersRZ",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"R_front_weapon_slot_01_ADJ", "R_front_weapon_slot_02_ADJ"}}},

                    { "IO_weapon_in_holstersLX",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"L_front_weapon_slot_04_ADJ", "L_front_weapon_slot_05_ADJ"}}},

                    { "IO_weapon_in_holstersLY",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"L_front_weapon_slot_04_ADJ", "L_front_weapon_slot_05_ADJ"}}},

                    { "IO_weapon_in_holstersLZ",
                        new PartDataStruct{ value = 0, min = -2, max = 2, bones = new List<string>{"L_front_weapon_slot_04_ADJ", "L_front_weapon_slot_05_ADJ"}}},

                    { "IS_cloak",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"C_back_weapon_slot_08_ADJ"}}},

                    { "IS_backpack",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"C_back_w_____slot_08"}}},

                    { "IS_weapon_in_hand",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_WeaponBone", "L_WeaponBone"}}},

                    { "IS_weapon_in_holsters",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_front_weapon_slot_01", "R_front_weapon_slot_02", "C_front_weapon_slot_03", "L_front_weapon_slot_04", "L_front_weapon_slot_05"}}},

                    { "IS_back_weapon_R",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"R_back_weapon_slot_06", "R_back_weapon_slot_09"}}},

                    { "IS_back_weapon_L",
                        new PartDataStruct{ value = 1, min = 0.5f, max = 2, bones = new List<string>{"L_back_weapon_slot_07", "L_back_weapon_slot_10"}}},
                };

                boneActions = new Dictionary<string, Func<Skeleton.BoneData, BoneDataStruct, float, Skeleton.BoneData>> {

                    { "OF_positionZ",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + param * -0.1f; return bone; }},

                    { "OF_shouldersX",
                        (bone, data, param) => { bone.Offset.z = data.originalValue.z + (data.boneName.StartsWith("R_") ? param * 0.1f : param * -0.1f); return bone; }},

                    { "OF_shouldersZ",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + (data.boneName.StartsWith("R_") ? param * -0.1f : param * -0.1f); return bone; }},

                    { "OF_upper_armsX",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + (data.boneName.StartsWith("R_") ? param * -0.1f : param * 0.1f); return bone; }},

                    { "OF_upper_legsX",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + (data.boneName.StartsWith("R_") ? param * 0.1f : param * -0.1f); return bone; }},

                    { "SC_pelvisX",
                        (bone, data, param) => { bone.Scale.x = data.originalValue.x * param; return bone; }},

                    { "SC_pelvisY",
                        (bone, data, param) => { bone.Scale.y = data.originalValue.y * param; return bone; }},

                    { "SC_pelvisZ",
                        (bone, data, param) => { bone.Scale.z = data.originalValue.z * param; return bone; }},

                    { "IO_cloakX",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + param * -0.1f; return bone; }},

                    { "IO_cloakY",
                        (bone, data, param) => { bone.Offset.y = data.originalValue.y + param * -0.1f; return bone; }},

                    { "IO_cloakZ",
                        (bone, data, param) => { bone.Offset.z = data.originalValue.z + param * -0.1f; return bone; }},

                    { "IO_backpackX",
                        (bone, data, param) => { bone.Offset.z = data.originalValue.z + param * -0.1f; return bone; }},

                    { "IO_backpackY",
                        (bone, data, param) => { bone.Offset.y = data.originalValue.y + param * -0.1f; return bone; }},

                    { "IO_backpackZ",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + param * -0.1f; return bone; }},

                    { "IO_weapon_in_holstersRX",
                        (bone, data, param) => { bone.Offset.y = data.originalValue.y + param * -0.1f; return bone; }},

                    { "IO_weapon_in_holstersRY",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + param * 0.1f; return bone; }},

                    { "IO_weapon_in_holstersRZ",
                        (bone, data, param) => { bone.Offset.z = data.originalValue.z + param * -0.1f; return bone; }},

                    { "IO_weapon_in_holstersLX",
                        (bone, data, param) => { bone.Offset.y = data.originalValue.y + param * -0.1f; return bone; }},

                    { "IO_weapon_in_holstersLY",
                        (bone, data, param) => { bone.Offset.x = data.originalValue.x + param * -0.1f; return bone; }},

                    { "IO_weapon_in_holstersLZ",
                        (bone, data, param) => { bone.Offset.z = data.originalValue.z + param * -0.1f; return bone; }},

                    { "default",
                        (bone, data, param) => { bone.Scale = data.originalValue * param; return bone; }},
                };

                CreateBodyParts(partsTable, bodyParts);
            }
        }

        private void CreateBodyParts(Dictionary<string, PartDataStruct> bodyPartsTable, Dictionary<string, BodyPart> bodyParts) {

            foreach (string key in bodyPartsTable.Keys) {

                switch (key) {

                    case string s when s.StartsWith("OF_"):

                        groupOF[key] = new BodyPart(bodyPartsTable[key].value, bodyPartsTable[key].min, bodyPartsTable[key].max);
                        bodyParts[key] = groupOF[key];
                        break;

                    case string s when s.StartsWith("SC_"):

                        groupSC[key] = new BodyPart(bodyPartsTable[key].value, bodyPartsTable[key].min, bodyPartsTable[key].max);
                        bodyParts[key] = groupSC[key];
                        break;

                    case string s when s.StartsWith("SZ_"):

                        groupSZ[key] = new BodyPart(bodyPartsTable[key].value, bodyPartsTable[key].min, bodyPartsTable[key].max);
                        bodyParts[key] = groupSZ[key];
                        break;

                    case string s when s.StartsWith("IO_"):

                        groupIO[key] = new BodyPart(bodyPartsTable[key].value, bodyPartsTable[key].min, bodyPartsTable[key].max);
                        bodyParts[key] = groupIO[key];
                        break;

                    case string s when s.StartsWith("IS_"):

                        groupIS[key] = new BodyPart(bodyPartsTable[key].value, bodyPartsTable[key].min, bodyPartsTable[key].max);
                        bodyParts[key] = groupIS[key];
                        break;

                    default:

                        bodyParts[key] = new BodyPart(bodyPartsTable[key].value, bodyPartsTable[key].min, bodyPartsTable[key].max);
                        break;
                }

                var isOffsetPart = groupOF.ContainsKey(key) || groupIO.ContainsKey(key);

                foreach (string value in bodyPartsTable[key].bones) {

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
            tempSkeleton.m_BonesByName = skeleton.m_BonesByName;
            tempSkeleton.m_IsDirty = skeleton.m_IsDirty;

            return tempSkeleton;
        }

        private void UpdateWeaponSizes() {

            var m1 = bodyParts.ContainsKey("SC_lower_torso") ? bodyParts["SC_lower_torso"].parameter : 1;
            var m2 = bodyParts.ContainsKey("SC_middle_torso") ? bodyParts["SC_middle_torso"].parameter : 1;
            var m3 = bodyParts.ContainsKey("SC_upper_torso") ? bodyParts["SC_upper_torso"].parameter : 1;
            var m4 = bodyParts.ContainsKey("SC_shoulders") ? bodyParts["SC_shoulders"].parameter : 1;
            var m5 = bodyParts.ContainsKey("SC_upper_arms") ? bodyParts["SC_upper_arms"].parameter : 1;
            var m6 = bodyParts.ContainsKey("SC_fore_arms") ? bodyParts["SC_fore_arms"].parameter : 1;
            var m7 = bodyParts.ContainsKey("SZ_hands") ? bodyParts["SZ_hands"].parameter : 1;

            var M1 = m1 * m2 * m3;
            var M2 = M1 * m4 * m5 * m6 * m7;

            UpdateWeapon("IS_weapon_in_hand", M2);
            UpdateWeapon("IS_back_weapon_R", M1);
            UpdateWeapon("IS_back_weapon_L", M1);
        }

        private void UpdateWeapon(string part, float multiplier) {

            if (bodyParts.ContainsKey(part)) {

                foreach (BoneDataStruct bone in bodyParts[part].bonesData) {

                    var tarrgetBone = newSkeleton.m_BoneDataForJob[bone.boneIndex];

                    tarrgetBone.Scale = (bone.originalValue * bodyParts[part].parameter) / multiplier;
                    newSkeleton.m_BoneDataForJob[bone.boneIndex] = tarrgetBone;
                }
            }
        }

        private void BonesModification(Dictionary<string, float> loadedData, bool load, string part) {

            float parameter;

            if (load && loadedData.ContainsKey(part)) {

                parameter = loadedData[part];
                bodyParts[part].parameter = parameter;

            } else {

                parameter = bodyParts[part].parameter;
                loadedData[part] = parameter;
            }

            foreach (BoneDataStruct bone in bodyParts[part].bonesData) {

                var targetBone = newSkeleton.m_BoneDataForJob[bone.boneIndex];

                if (boneActions.ContainsKey(part)) {

                    targetBone.ApplyOffset = bone.applyOffset;
                    targetBone = boneActions[part](targetBone, bone, parameter);

                } else {

                    targetBone = boneActions["default"](targetBone, bone, parameter);
                }

                newSkeleton.m_BoneDataForJob[bone.boneIndex] = targetBone;
            }
        }

        public void ApplyBonesModification(BaseUnitEntity character, bool loadPerSaveData = true, string whichPart = "all") {

            if (character?.HashKey() == owner && character?.View?.CharacterAvatar?.Skeleton is Skeleton skeleton) {

                if (skeleton != newSkeleton) {

                    character.View.CharacterAvatar.Skeleton = newSkeleton;

                    foreach (EquipmentEntity item in character.View.CharacterAvatar.EquipmentEntities) {

                        foreach (EquipmentEntity.OutfitPart part in item.OutfitParts) {

                            if (part.Special == EquipmentEntity.OutfitPartSpecialType.Backpack) {

                                part.m_BoneName = "C_back_weapon_slot_08";
                                character.View.CharacterAvatar.RebuildOutfit();
                            }
                        }
                    }
                }

                var loadedPartsData = Main.Settings.perSave.characterSkeletonReplacers;

                if (!loadedPartsData.ContainsKey(character.HashKey())) {

                    loadedPartsData[character.HashKey()] = new Dictionary<string, float>();
                }

                if (bodyParts.ContainsKey(whichPart)) {

                    BonesModification(loadedPartsData[character.HashKey()], loadPerSaveData, whichPart);

                } else {

                    foreach (string key in bodyParts.Keys) {

                        BonesModification(loadedPartsData[character.HashKey()], loadPerSaveData, key);
                    }
                }

                UpdateWeaponSizes();

                if (loadPerSaveData) {

                    foreach (string key in loadedPartsData[character.HashKey()].Keys) {

                        if (!bodyParts.ContainsKey(key)) {

                            loadedPartsData[character.HashKey()].Remove(key);
                        }
                    }
                }
            }
        }

        public class BodyPart {

            public bool isEmpty = true;
            public float parameter;
            public float min;
            public float max;
            public List<BoneDataStruct> bonesData;

            public BodyPart(float defaultParameter, float minParameter, float maxParameter) {

                parameter = defaultParameter;
                min = minParameter;
                max = maxParameter;
                bonesData = new List<BoneDataStruct>();
            }
        }

        public struct PartDataStruct {

            public float value;
            public float min;
            public float max;
            public List<string> bones;
        }

        public struct BoneDataStruct {

            public string boneName;
            public int boneIndex;
            public bool applyOffset;
            public Vector3 originalValue;
        }
    }
}
