using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Configures the GanzSe FBX as a proper Humanoid avatar with bone mapping.
/// Maps GanzSe bone names (spine_01, upperarm_l, etc.) to Unity Humanoid standard.
/// Run once at setup — the avatar persists in the .meta file.
/// </summary>
public static class GanzSeAvatarSetup
{
    private static readonly string GanzseFbxPath =
        "Assets/Free Low Poly Modular Character Pack - Fantasy Dream/Models/Models Update 1.1/GanzSe Free Modular Character 1_1.fbx";

    [MenuItem("DonGeonMaster/Configure GanzSe Avatar")]
    public static void ConfigureAvatar()
    {
        var importer = AssetImporter.GetAtPath(GanzseFbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("[AvatarSetup] GanzSe FBX not found at: " + GanzseFbxPath);
            return;
        }

        // Check if already configured (human[] populated)
        if (importer.humanDescription.human != null && importer.humanDescription.human.Length > 10)
        {
            Debug.Log("[AvatarSetup] GanzSe avatar already configured.");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;

        var humanDesc = importer.humanDescription;

        // Build HumanBone mappings: GanzSe bone name → Unity Humanoid name
        var bones = new List<HumanBone>();

        void Map(string ganzseBone, string humanName)
        {
            bones.Add(new HumanBone
            {
                boneName = ganzseBone,
                humanName = humanName,
                limit = new HumanLimit { useDefaultValues = true }
            });
        }

        // Core skeleton
        Map("spine_01", "Hips");        // spine_01 acts as Hips (root of skeleton)
        Map("spine_02", "Spine");
        Map("spine_03", "Chest");
        Map("spine_04", "UpperChest");
        Map("neck", "Neck");
        Map("head", "Head");

        // Left arm
        Map("shoulder_l", "LeftShoulder");
        Map("upperarm_l", "LeftUpperArm");
        Map("forearm_l", "LeftLowerArm");
        Map("hand_l", "LeftHand");

        // Right arm
        Map("shoulder_r", "RightShoulder");
        Map("upperarm_r", "RightUpperArm");
        Map("forearm_r", "RightLowerArm");
        Map("hand_r", "RightHand");

        // Left leg
        Map("upperleg_l", "LeftUpperLeg");
        Map("shin_l", "LeftLowerLeg");
        Map("foot_l", "LeftFoot");
        Map("toes_l", "LeftToes");

        // Right leg
        Map("upperleg_r", "RightUpperLeg");
        Map("shin_r", "RightLowerLeg");
        Map("foot_r", "RightFoot");
        Map("toes_r", "RightToes");

        // Left hand fingers
        Map("thumb_01_l", "Left Thumb Proximal");
        Map("thumb_02_l", "Left Thumb Intermediate");
        Map("thumb_03_l", "Left Thumb Distal");
        Map("index_01_l", "Left Index Proximal");
        Map("index_02_l", "Left Index Intermediate");
        Map("index_03_l", "Left Index Distal");
        Map("middle_01_l", "Left Middle Proximal");
        Map("middle_02_l", "Left Middle Intermediate");
        Map("middle_03_l", "Left Middle Distal");
        Map("ring_01_l", "Left Ring Proximal");
        Map("ring_02_l", "Left Ring Intermediate");
        Map("ring_03_l", "Left Ring Distal");
        Map("pinky_01_l", "Left Little Proximal");
        Map("pinky_02_l", "Left Little Intermediate");
        Map("pinky_03_l", "Left Little Distal");

        // Right hand fingers
        Map("thumb_01_r", "Right Thumb Proximal");
        Map("thumb_02_r", "Right Thumb Intermediate");
        Map("thumb_03_r", "Right Thumb Distal");
        Map("index_01_r", "Right Index Proximal");
        Map("index_02_r", "Right Index Intermediate");
        Map("index_03_r", "Right Index Distal");
        Map("middle_01_r", "Right Middle Proximal");
        Map("middle_02_r", "Right Middle Intermediate");
        Map("middle_03_r", "Right Middle Distal");
        Map("ring_01_r", "Right Ring Proximal");
        Map("ring_02_r", "Right Ring Intermediate");
        Map("ring_03_r", "Right Ring Distal");
        Map("pinky_01_r", "Right Little Proximal");
        Map("pinky_02_r", "Right Little Intermediate");
        Map("pinky_03_r", "Right Little Distal");

        humanDesc.human = bones.ToArray();
        humanDesc.hasTranslationDoF = false;
        humanDesc.armStretch = 0.05f;
        humanDesc.legStretch = 0.05f;
        humanDesc.feetSpacing = 0;

        importer.humanDescription = humanDesc;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        // Force sync reimport
        AssetDatabase.ImportAsset(GanzseFbxPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"[AvatarSetup] GanzSe avatar configured with {bones.Count} bone mappings.");
    }
}
