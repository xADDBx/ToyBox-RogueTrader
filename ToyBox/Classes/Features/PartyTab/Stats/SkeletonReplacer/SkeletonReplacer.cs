using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.CharacterSystem;
using Unity.Collections;
using UnityEngine;

namespace ToyBox.Features.PartyTab.Stats;

internal class SkeletonReplacer {
    private readonly string m_Owner;
    private readonly WeakReference<Character> m_Avatar;
    private readonly Skeleton m_OldSkeleton;
    private readonly Skeleton m_NewSkeleton;

    public readonly Dictionary<string, BodyPart> BodyParts = [];
    public readonly Dictionary<string, BodyPart> GroupOF = [];
    public readonly Dictionary<string, BodyPart> GroupSC = [];
    public readonly Dictionary<string, BodyPart> GroupSZ = [];
    public readonly Dictionary<string, BodyPart> GroupIO = [];
    public readonly Dictionary<string, BodyPart> GroupIS = [];

    private readonly Dictionary<string, Func<Skeleton.BoneData, BoneDataStruct, float, Skeleton.BoneData>> m_BoneActions;

    public bool IsValid { get; private set; }

    public SkeletonReplacer(BaseUnitEntity character) {
        if (character?.View?.CharacterAvatar is not Character avatar || avatar.Skeleton is not Skeleton skeleton) {
            m_Owner = "";
            m_Avatar = new(null!);
            m_OldSkeleton = null!;
            m_NewSkeleton = null!;
            m_BoneActions = [];
            return;
        }
        m_Owner = character.UniqueId;
        m_Avatar = new(avatar);
        m_OldSkeleton = skeleton;
        m_NewSkeleton = DuplicateSkeleton(skeleton);

        var partsTable = new Dictionary<string, PartDataStruct> {
            { "OF_positionZ", new() { Value = 0, Min = -10, Max = 10, Bones = ["Position"] } },
            { "OF_shouldersX", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_Clavicle", "L_Clavicle"] } },
            { "OF_shouldersZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_Clavicle", "L_Clavicle"] } },
            { "OF_upper_armsX", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_Up_arm", "L_Up_arm"] } },
            { "OF_upper_legsX", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_Pre_Up_Leg", "L_Pre_Up_Leg"] } },
            { "SC_pelvisX", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Pelvis"] } },
            { "SC_pelvisY", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Pelvis"] } },
            { "SC_pelvisZ", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Pelvis"] } },
            { "SC_neck", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Neck"] } },
            { "SC_shoulders", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Clavicle", "L_Clavicle"] } },
            { "SC_upper_arms", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Up_arm", "L_Up_arm"] } },
            { "SC_fore_arms", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_ForeArm", "L_ForeArm"] } },
            { "SC_upper_torso", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Spine_3"] } },
            { "SC_middle_torso", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Spine_2"] } },
            { "SC_lower_torso", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Spine_1"] } },
            { "SC_stomach", new() { Value = 1, Min = 0.2f, Max = 5, Bones = ["Stomach"] } },
            { "SC_upper_legs", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Pre_Up_Leg", "L_Pre_Up_Leg"] } },
            { "SC_lower_legs", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Up_leg", "L_Up_leg"] } },
            { "SC_foots", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_foot", "L_foot"] } },
            { "SC_toes", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_toe", "L_toe"] } },
            { "SZ_head", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Head"] } },
            { "SZ_neck", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Neck_ADJ"] } },
            { "SZ_shoulders", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Clavicle_ADJ", "L_Clavicle_ADJ"] } },
            { "SZ_upper_arms", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Up_arm_ADJ", "L_Up_arm_ADJ"] } },
            { "SZ_fore_arms", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_ForeArm_ADJ", "R_ForeArm_Twist_ADJ", "L_ForeArm_ADJ", "L_ForeArm_Twist_ADJ"] } },
            { "SZ_hands", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Hand", "L_Hand"] } },
            { "SZ_upper_torso", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Spine_3_ADJ"] } },
            { "SZ_middle_torso", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Spine_2_ADJ"] } },
            { "SZ_lower_torso", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Spine_1_ADJ"] } },
            { "SZ_stomach", new() { Value = 1, Min = 0.2f, Max = 5, Bones = ["Stomach_ADJ"] } },
            { "SZ_pelvis", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["Pelvis_ADJ"] } },
            { "SZ_upper_legs", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Up_leg_ADJ", "L_Up_leg_ADJ"] } },
            { "SZ_middle_legs", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_leg_ADJ", "L_leg_ADJ"] } },
            { "SZ_lower_legs", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_Ankle_ADJ", "L_Ankle_ADJ"] } },
            { "SZ_foots", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_foot_ADJ", "L_foot_ADJ"] } },
            { "SZ_toes", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_toe_ADJ", "L_toe_ADJ"] } },
            { "IO_cloakX", new() { Value = 0, Min = -2, Max = 2, Bones = ["C_back_weapon_slot_08_ADJ"] } },
            { "IO_cloakY", new() { Value = 0, Min = -2, Max = 2, Bones = ["C_back_weapon_slot_08_ADJ"] } },
            { "IO_cloakZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["C_back_weapon_slot_08_ADJ"] } },
            { "IO_backpackX", new() { Value = 0, Min = -2, Max = 2, Bones = ["C_back_w_____slot_08"] } },
            { "IO_backpackY", new() { Value = 0, Min = -2, Max = 2, Bones = ["C_back_w_____slot_08"] } },
            { "IO_backpackZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["C_back_w_____slot_08"] } },
            { "IO_weapon_in_hand_RX", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IO_weapon_in_hand_RY", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IO_weapon_in_hand_RZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IO_weapon_in_hand_LX", new() { Value = 0, Min = -2, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IO_weapon_in_hand_LY", new() { Value = 0, Min = -2, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IO_weapon_in_hand_LZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IO_weapon_in_holstersRX", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_front_weapon_slot_01_ADJ", "R_front_weapon_slot_02_ADJ"] } },
            { "IO_weapon_in_holstersRY", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_front_weapon_slot_01_ADJ", "R_front_weapon_slot_02_ADJ"] } },
            { "IO_weapon_in_holstersRZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["R_front_weapon_slot_01_ADJ", "R_front_weapon_slot_02_ADJ"] } },
            { "IO_weapon_in_holstersLX", new() { Value = 0, Min = -2, Max = 2, Bones = ["L_front_weapon_slot_04_ADJ", "L_front_weapon_slot_05_ADJ"] } },
            { "IO_weapon_in_holstersLY", new() { Value = 0, Min = -2, Max = 2, Bones = ["L_front_weapon_slot_04_ADJ", "L_front_weapon_slot_05_ADJ"] } },
            { "IO_weapon_in_holstersLZ", new() { Value = 0, Min = -2, Max = 2, Bones = ["L_front_weapon_slot_04_ADJ", "L_front_weapon_slot_05_ADJ"] } },
            { "IS_cloak", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["C_back_weapon_slot_08_ADJ"] } },
            { "IS_backpack", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["C_back_w_____slot_08"] } },
            { "IS_weapon_in_hand_R", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IS_weapon_in_hand_L", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IS_weapon_in_hand_RX", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IS_weapon_in_hand_RY", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IS_weapon_in_hand_RZ", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_WeaponBone"] } },
            { "IS_weapon_in_hand_LX", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IS_weapon_in_hand_LY", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IS_weapon_in_hand_LZ", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["L_WeaponBone"] } },
            { "IS_weapon_in_holsters", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_front_weapon_slot_01", "R_front_weapon_slot_02", "C_front_weapon_slot_03", "L_front_weapon_slot_04", "L_front_weapon_slot_05"] } },
            { "IS_back_weapon_R", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["R_back_weapon_slot_06", "R_back_weapon_slot_09"] } },
            { "IS_back_weapon_L", new() { Value = 1, Min = 0.5f, Max = 2, Bones = ["L_back_weapon_slot_07", "L_back_weapon_slot_10"] } },
        };

        m_BoneActions = new() {
            { "OF_positionZ", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * -0.1f); return bone; } },
            { "OF_shouldersX", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (data.BoneName.StartsWith("R_") ? param * 0.1f : param * -0.1f); return bone; } },
            { "OF_shouldersZ", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (data.BoneName.StartsWith("R_") ? param * -0.1f : param * -0.1f); return bone; } },
            { "OF_upper_armsX", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (data.BoneName.StartsWith("R_") ? param * -0.1f : param * 0.1f); return bone; } },
            { "OF_upper_legsX", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (data.BoneName.StartsWith("R_") ? param * 0.1f : param * -0.1f); return bone; } },
            { "SC_pelvisX", (bone, data, param) => { bone.Scale.x = data.OriginalValue.x * param; return bone; } },
            { "SC_pelvisY", (bone, data, param) => { bone.Scale.y = data.OriginalValue.y * param; return bone; } },
            { "SC_pelvisZ", (bone, data, param) => { bone.Scale.z = data.OriginalValue.z * param; return bone; } },
            { "IO_cloakX", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * -0.1f); return bone; } },
            { "IO_cloakY", (bone, data, param) => { bone.Offset.y = data.OriginalValue.y + (param * -0.1f); return bone; } },
            { "IO_cloakZ", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (param * -0.1f); return bone; } },
            { "IO_backpackX", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (param * -0.1f); return bone; } },
            { "IO_backpackY", (bone, data, param) => { bone.Offset.y = data.OriginalValue.y + (param * -0.1f); return bone; } },
            { "IO_backpackZ", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * -0.1f); return bone; } },
            { "IO_weapon_in_hand_RX", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * 0.1f); return bone; } },
            { "IO_weapon_in_hand_RY", (bone, data, param) => { bone.Offset.y = data.OriginalValue.y + (param * 0.1f); return bone; } },
            { "IO_weapon_in_hand_RZ", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (param * 0.1f); return bone; } },
            { "IO_weapon_in_hand_LX", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * 0.1f); return bone; } },
            { "IO_weapon_in_hand_LY", (bone, data, param) => { bone.Offset.y = data.OriginalValue.y + (param * 0.1f); return bone; } },
            { "IO_weapon_in_hand_LZ", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (param * 0.1f); return bone; } },
            { "IO_weapon_in_holstersRX", (bone, data, param) => { bone.Offset.y = data.OriginalValue.y + (param * -0.1f); return bone; } },
            { "IO_weapon_in_holstersRY", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * 0.1f); return bone; } },
            { "IO_weapon_in_holstersRZ", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (param * -0.1f); return bone; } },
            { "IO_weapon_in_holstersLX", (bone, data, param) => { bone.Offset.y = data.OriginalValue.y + (param * -0.1f); return bone; } },
            { "IO_weapon_in_holstersLY", (bone, data, param) => { bone.Offset.x = data.OriginalValue.x + (param * -0.1f); return bone; } },
            { "IO_weapon_in_holstersLZ", (bone, data, param) => { bone.Offset.z = data.OriginalValue.z + (param * -0.1f); return bone; } },
            { "default", (bone, data, param) => { bone.Scale = data.OriginalValue * param; return bone; } },
        };

        CreateBodyParts(partsTable);
        IsValid = true;
    }

    public bool IsFor(BaseUnitEntity character) {
        return character.UniqueId == m_Owner && m_Avatar.TryGetTarget(out var avatar) && ReferenceEquals(character.View?.CharacterAvatar, avatar);
    }

    private void CreateBodyParts(Dictionary<string, PartDataStruct> bodyPartsTable) {
        foreach (var key in bodyPartsTable.Keys) {
            var group = key switch {
                _ when key.StartsWith("OF_") => GroupOF,
                _ when key.StartsWith("SC_") => GroupSC,
                _ when key.StartsWith("SZ_") => GroupSZ,
                _ when key.StartsWith("IO_") => GroupIO,
                _ when key.StartsWith("IS_") => GroupIS,
                _ => null,
            };
            var part = new BodyPart(bodyPartsTable[key].Value, bodyPartsTable[key].Min, bodyPartsTable[key].Max);
            BodyParts[key] = part;
            if (group != null) {
                group[key] = part;
            }
            var isOffsetPart = GroupOF.ContainsKey(key) || GroupIO.ContainsKey(key);
            foreach (var boneName in bodyPartsTable[key].Bones) {
                if (m_OldSkeleton.BonesByName.TryGetValue(boneName, out var bone)) {
                    part.IsEmpty = false;
                    var index = m_OldSkeleton.Bones.IndexOf(bone);
                    m_OldSkeleton.Bones[index].ApplyOffset = isOffsetPart;
                    var originalValue = isOffsetPart ? m_OldSkeleton.Bones[index].Offset : m_OldSkeleton.Bones[index].Scale;
                    part.BonesData.Add(new() { BoneName = boneName, BoneIndex = index, ApplyOffset = isOffsetPart, OriginalValue = originalValue });
                }
            }
        }
    }

    private static Skeleton DuplicateSkeleton(Skeleton skeleton) {
        var tempSkeleton = new Skeleton();
        var newBoneJobArray = new NativeArray<Skeleton.BoneData>(skeleton.Bones.Count, Allocator.Persistent);
        for (var i = 0; i < skeleton.Bones.Count; i++) {
            newBoneJobArray[i] = new() { ApplyOffset = skeleton.Bones[i].ApplyOffset, Offset = skeleton.Bones[i].Offset, Scale = skeleton.Bones[i].Scale };
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
        var m1 = BodyParts.TryGetValue("SC_lower_torso", out var p1) ? p1.Parameter : 1;
        var m2 = BodyParts.TryGetValue("SC_middle_torso", out var p2) ? p2.Parameter : 1;
        var m3 = BodyParts.TryGetValue("SC_upper_torso", out var p3) ? p3.Parameter : 1;
        var m4 = BodyParts.TryGetValue("SC_shoulders", out var p4) ? p4.Parameter : 1;
        var m5 = BodyParts.TryGetValue("SC_upper_arms", out var p5) ? p5.Parameter : 1;
        var m6 = BodyParts.TryGetValue("SC_fore_arms", out var p6) ? p6.Parameter : 1;
        var m7 = BodyParts.TryGetValue("SZ_hands", out var p7) ? p7.Parameter : 1;
        var big = m1 * m2 * m3;
        var full = big * m4 * m5 * m6 * m7;
        UpdateWeapon("IS_weapon_in_hand_R", full);
        UpdateWeapon("IS_weapon_in_hand_L", full);
        UpdateWeapon("IS_back_weapon_R", big);
        UpdateWeapon("IS_back_weapon_L", big);
    }

    private void UpdateWeapon(string part, float multiplier) {
        if (!BodyParts.TryGetValue(part, out var bodyPart)) {
            return;
        }
        foreach (var bone in bodyPart.BonesData) {
            var targetBone = m_NewSkeleton.m_BoneDataForJob[bone.BoneIndex];
            var axisScale = new Vector3(GetParameter($"{part}X"), GetParameter($"{part}Y"), GetParameter($"{part}Z"));
            targetBone.Scale = Vector3.Scale(bone.OriginalValue, axisScale) * bodyPart.Parameter / multiplier;
            m_NewSkeleton.m_BoneDataForJob[bone.BoneIndex] = targetBone;
        }
    }

    private float GetParameter(string part) {
        return BodyParts.TryGetValue(part, out var bodyPart) ? bodyPart.Parameter : 1;
    }

    private void BonesModification(Dictionary<string, float> loadedData, bool load, string part) {
        float parameter;
        if (load && loadedData.TryGetValue(part, out var stored)) {
            parameter = stored;
            BodyParts[part].Parameter = parameter;
        } else {
            parameter = BodyParts[part].Parameter;
            loadedData[part] = parameter;
        }
        foreach (var bone in BodyParts[part].BonesData) {
            var targetBone = m_NewSkeleton.m_BoneDataForJob[bone.BoneIndex];
            if (m_BoneActions.TryGetValue(part, out var action)) {
                targetBone.ApplyOffset = bone.ApplyOffset;
                targetBone = action(targetBone, bone, parameter);
            } else {
                targetBone = m_BoneActions["default"](targetBone, bone, parameter);
            }
            m_NewSkeleton.m_BoneDataForJob[bone.BoneIndex] = targetBone;
        }
    }

    public void ApplyBonesModification(BaseUnitEntity character, bool loadPerSaveData = true, string whichPart = "all") {
        if (!IsValid || character?.UniqueId != m_Owner || character?.View?.CharacterAvatar is not Character avatar) {
            return;
        }
        if (avatar.Skeleton != m_NewSkeleton) {
            avatar.Skeleton = m_NewSkeleton;
            foreach (var item in avatar.EquipmentEntities) {
                foreach (var part in item.OutfitParts) {
                    if (part.Special == EquipmentEntity.OutfitPartSpecialType.Backpack) {
                        part.m_BoneName = "C_back_weapon_slot_08";
                        avatar.RebuildOutfit();
                    }
                }
            }
        }
        var overrides = InSaveSettings?.SkeletonBoneOverrides;
        if (overrides == null) {
            return;
        }
        if (!overrides.TryGetValue(character.UniqueId, out var loadedPartsData)) {
            loadedPartsData = [];
            overrides[character.UniqueId] = loadedPartsData;
        }
        if (loadedPartsData.TryGetValue("IS_weapon_in_hand", out var legacyWeaponInHand)) {
            _ = loadedPartsData.Remove("IS_weapon_in_hand");
            if (!loadedPartsData.ContainsKey("IS_weapon_in_hand_R")) {
                loadedPartsData["IS_weapon_in_hand_R"] = legacyWeaponInHand;
            }
            if (!loadedPartsData.ContainsKey("IS_weapon_in_hand_L")) {
                loadedPartsData["IS_weapon_in_hand_L"] = legacyWeaponInHand;
            }
        }
        if (BodyParts.ContainsKey(whichPart)) {
            BonesModification(loadedPartsData, loadPerSaveData, whichPart);
        } else {
            foreach (var key in BodyParts.Keys) {
                BonesModification(loadedPartsData, loadPerSaveData, key);
            }
        }
        UpdateWeaponSizes();
        if (loadPerSaveData) {
            foreach (var key in loadedPartsData.Keys.ToList()) {
                if (!BodyParts.ContainsKey(key)) {
                    _ = loadedPartsData.Remove(key);
                }
            }
        }
        avatar.CacheSkeletonBones();
    }

    public class BodyPart {
        public bool IsEmpty = true;
        public float Parameter;
        public readonly float Min;
        public readonly float Max;
        public readonly List<BoneDataStruct> BonesData = [];
        public BodyPart(float defaultParameter, float minParameter, float maxParameter) {
            Parameter = defaultParameter;
            Min = minParameter;
            Max = maxParameter;
        }
    }

    public struct PartDataStruct {
        public float Value;
        public float Min;
        public float Max;
        public List<string> Bones;
    }

    public struct BoneDataStruct {
        public string BoneName;
        public int BoneIndex;
        public bool ApplyOffset;
        public Vector3 OriginalValue;
    }
}
