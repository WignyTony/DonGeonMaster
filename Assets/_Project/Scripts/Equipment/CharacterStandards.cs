using UnityEngine;

namespace DonGeonMaster.Equipment
{
    /// <summary>
    /// Standard dimensions and naming conventions for the character and equipment system.
    /// All measurements in meters (1 Unity unit = 1 meter).
    /// All equipment must conform to these standards to fit the character.
    /// </summary>
    public static class CharacterStandards
    {
        // =====================================================================
        // CHARACTER DIMENSIONS
        // =====================================================================
        public const float CharacterHeight = 1.8f;
        public const float HeadHeight = 0.23f;
        public const float NeckHeight = 0.08f;
        public const float TorsoHeight = 0.55f;   // Hips to shoulders
        public const float LegLength = 0.85f;     // Hip joint to ankle
        public const float ArmLength = 0.58f;     // Shoulder to wrist
        public const float ShoulderWidth = 0.42f;  // Full span between shoulders
        public const float HipWidth = 0.28f;
        public const float FootLength = 0.26f;

        // =====================================================================
        // BONE NAMES (must match Blender armature export)
        // =====================================================================
        public const string Bone_Hips = "Hips";
        public const string Bone_Spine = "Spine";
        public const string Bone_Spine1 = "Spine1";
        public const string Bone_Spine2 = "Spine2";
        public const string Bone_Neck = "Neck";
        public const string Bone_Head = "Head";

        public const string Bone_Shoulder_L = "Shoulder.L";
        public const string Bone_UpperArm_L = "UpperArm.L";
        public const string Bone_LowerArm_L = "LowerArm.L";
        public const string Bone_Hand_L = "Hand.L";

        public const string Bone_Shoulder_R = "Shoulder.R";
        public const string Bone_UpperArm_R = "UpperArm.R";
        public const string Bone_LowerArm_R = "LowerArm.R";
        public const string Bone_Hand_R = "Hand.R";

        public const string Bone_UpperLeg_L = "UpperLeg.L";
        public const string Bone_LowerLeg_L = "LowerLeg.L";
        public const string Bone_Foot_L = "Foot.L";

        public const string Bone_UpperLeg_R = "UpperLeg.R";
        public const string Bone_LowerLeg_R = "LowerLeg.R";
        public const string Bone_Foot_R = "Foot.R";

        // =====================================================================
        // EQUIPMENT SOCKET BONES
        // These are empty bones used to attach equipment meshes at runtime.
        // =====================================================================
        public const string Socket_Head = "Socket_Head";       // Helmets, hats
        public const string Socket_Chest = "Socket_Chest";     // Chest armor
        public const string Socket_Hand_R = "Socket_Hand_R";   // Weapon (right hand)
        public const string Socket_Hand_L = "Socket_Hand_L";   // Shield / offhand
        public const string Socket_Back = "Socket_Back";       // Cape, backpack
        public const string Socket_Foot_L = "Socket_Foot_L";   // Boot left
        public const string Socket_Foot_R = "Socket_Foot_R";   // Boot right

        // =====================================================================
        // EQUIPMENT SLOTS
        // =====================================================================
        public enum EquipmentSlot
        {
            Head,        // GanzSe: HEADS
            Chest,       // GanzSe: CHEST ARMOR
            Legs,        // GanzSe: LEG ARMOR
            Feet,        // GanzSe: FEET ARMOR
            Belt,        // GanzSe: BELT ARMOR
            Arms,        // GanzSe: ARM ARMOR
            Weapon,      // Right hand weapon
            Shield,      // Left hand shield
            Back         // Cape, quiver (future)
        }

        public enum EquipmentWeight
        {
            TresLeger,
            Leger,
            Moyen,
            Lourd,
            TresLourd
        }

        public enum WeaponType
        {
            Epee,
            GrandeEpee,
            Dague,
            Marteau,
            Masse,
            Hache,
            GrandeHache,
            Bouclier,
            Baton,
            Lance
        }

        public enum ArmorMaterial
        {
            Tissu,
            Cuir,
            Mailles,
            Plaques,
            Ecailles,
            Os,
            Mystique
        }

        public enum Handling
        {
            TresRapide,
            Rapide,
            Normal,
            Lent,
            TresLent
        }

        public enum ElementType
        {
            Aucun,
            Feu,
            Glace,
            Foudre,
            Poison,
            Sacre,
            Tenebres,
            Arcane
        }

        // =====================================================================
        // EQUIPMENT BOUNDING BOXES (max dimensions for each slot)
        // Equipment meshes must fit within these bounds.
        // =====================================================================
        public static Vector3 GetSlotMaxBounds(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => new Vector3(0.35f, 0.35f, 0.35f),
                EquipmentSlot.Chest => new Vector3(0.50f, 0.60f, 0.30f),
                EquipmentSlot.Legs => new Vector3(0.40f, 0.85f, 0.30f),
                EquipmentSlot.Feet => new Vector3(0.14f, 0.20f, 0.30f),
                EquipmentSlot.Belt => new Vector3(0.35f, 0.15f, 0.25f),
                EquipmentSlot.Arms => new Vector3(0.15f, 0.30f, 0.15f),
                EquipmentSlot.Weapon => new Vector3(0.15f, 1.20f, 0.15f),
                EquipmentSlot.Shield => new Vector3(0.45f, 0.55f, 0.12f),
                EquipmentSlot.Back => new Vector3(0.40f, 0.60f, 0.20f),
                _ => Vector3.one
            };
        }

        // Socket bone name for each slot
        public static string GetSocketBone(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => Socket_Head,
                EquipmentSlot.Chest => Socket_Chest,
                EquipmentSlot.Weapon => Socket_Hand_R,
                EquipmentSlot.Shield => Socket_Hand_L,
                EquipmentSlot.Back => Socket_Back,
                EquipmentSlot.Feet => Socket_Foot_R,
                _ => null
            };
        }

        // =====================================================================
        // ANIMATION NAMES (must match Blender action names on FBX export)
        // =====================================================================
        public const string Anim_Idle = "Idle";
        public const string Anim_Walk = "Walk";
        public const string Anim_Run = "Run";
        public const string Anim_Attack = "Attack_1H";
        public const string Anim_Block = "Block";
        public const string Anim_Hit = "Hit";
        public const string Anim_Death = "Death";
        public const string Anim_Dodge = "Dodge";
        public const string Anim_Interact = "Interact";

        // =====================================================================
        // DEFAULT MESH NAMES (base clothing on the character)
        // =====================================================================
        public const string Mesh_Body = "Body";
        public const string Mesh_Tunic = "Tunic";
        public const string Mesh_Pants = "Pants";
        public const string Mesh_Boots = "Boots";
    }
}
