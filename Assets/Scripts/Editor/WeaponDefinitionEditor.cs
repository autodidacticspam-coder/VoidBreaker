using UnityEngine;
using UnityEditor;
using VoidBreaker.Data;

namespace VoidBreaker.Editor
{
    [CustomEditor(typeof(WeaponDefinition))]
    public class WeaponDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            WeaponDefinition weapon = (WeaponDefinition)target;

            // Header
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Weapon Definition", EditorStyles.boldLabel);
            if (weapon.icon != null)
            {
                GUILayout.Label(weapon.icon.texture, GUILayout.Height(64));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            weapon.weaponId = EditorGUILayout.TextField("Weapon ID", weapon.weaponId);
            weapon.weaponName = EditorGUILayout.TextField("Weapon Name", weapon.weaponName);
            weapon.weaponType = (WeaponType)EditorGUILayout.EnumPopup("Type", weapon.weaponType);
            weapon.rarity = (WeaponRarity)EditorGUILayout.EnumPopup("Rarity", weapon.rarity);

            EditorGUILayout.LabelField("Description");
            weapon.description = EditorGUILayout.TextArea(weapon.description, GUILayout.Height(40));

            EditorGUILayout.Space();

            // Combat Stats
            EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
            weapon.damage = EditorGUILayout.IntSlider("Damage", weapon.damage, 1, 10);
            weapon.shieldPiercing = EditorGUILayout.IntSlider("Shield Pierce", weapon.shieldPiercing, 0, 5);
            weapon.projectileCount = EditorGUILayout.IntSlider("Projectiles", weapon.projectileCount, 1, 8);
            weapon.projectileSpread = EditorGUILayout.Slider("Spread", weapon.projectileSpread, 0f, 1f);

            EditorGUILayout.Space();

            // Timing
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            weapon.chargeTime = EditorGUILayout.Slider("Charge Time", weapon.chargeTime, 1f, 30f);
            weapon.cooldown = EditorGUILayout.Slider("Cooldown", weapon.cooldown, 0f, 10f);

            EditorGUILayout.Space();

            // Resources
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            weapon.powerCost = EditorGUILayout.IntSlider("Power Cost", weapon.powerCost, 1, 5);
            weapon.ammoCost = EditorGUILayout.IntField("Ammo Cost", weapon.ammoCost);
            weapon.ammoType = (AmmoType)EditorGUILayout.EnumPopup("Ammo Type", weapon.ammoType);

            EditorGUILayout.Space();

            // Special Effects
            EditorGUILayout.LabelField("Special Effects", EditorStyles.boldLabel);
            weapon.causesBreaches = EditorGUILayout.Toggle("Causes Breaches", weapon.causesBreaches);
            weapon.breachChance = EditorGUILayout.Slider("Breach Chance", weapon.breachChance, 0f, 1f);
            weapon.causesFires = EditorGUILayout.Toggle("Causes Fires", weapon.causesFires);
            weapon.fireChance = EditorGUILayout.Slider("Fire Chance", weapon.fireChance, 0f, 1f);
            weapon.stunsCrew = EditorGUILayout.Toggle("Stuns Crew", weapon.stunsCrew);
            weapon.stunDuration = EditorGUILayout.Slider("Stun Duration", weapon.stunDuration, 0f, 10f);

            EditorGUILayout.Space();

            // System Targeting
            EditorGUILayout.LabelField("System Targeting", EditorStyles.boldLabel);
            weapon.systemDamageBonus = EditorGUILayout.IntField("System Damage Bonus", weapon.systemDamageBonus);
            weapon.ionDamage = EditorGUILayout.IntField("Ion Damage", weapon.ionDamage);

            EditorGUILayout.Space();

            // Shop
            EditorGUILayout.LabelField("Shop", EditorStyles.boldLabel);
            weapon.basePrice = EditorGUILayout.IntField("Base Price", weapon.basePrice);

            EditorGUILayout.Space();

            // Visuals
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            weapon.icon = (Sprite)EditorGUILayout.ObjectField("Icon", weapon.icon, typeof(Sprite), false);
            weapon.projectileSprite = (Sprite)EditorGUILayout.ObjectField("Projectile Sprite", weapon.projectileSprite, typeof(Sprite), false);
            weapon.projectileColor = EditorGUILayout.ColorField("Projectile Color", weapon.projectileColor);
            weapon.muzzleFlashPrefab = (GameObject)EditorGUILayout.ObjectField("Muzzle Flash", weapon.muzzleFlashPrefab, typeof(GameObject), false);

            EditorGUILayout.Space();

            // Audio
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            weapon.fireSound = (AudioClip)EditorGUILayout.ObjectField("Fire Sound", weapon.fireSound, typeof(AudioClip), false);
            weapon.impactSound = (AudioClip)EditorGUILayout.ObjectField("Impact Sound", weapon.impactSound, typeof(AudioClip), false);
            weapon.chargeSound = (AudioClip)EditorGUILayout.ObjectField("Charge Sound", weapon.chargeSound, typeof(AudioClip), false);

            // Preview section
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Weapon Preview", EditorStyles.boldLabel);

            float dps = weapon.damage * weapon.projectileCount / (weapon.chargeTime + weapon.cooldown);
            EditorGUILayout.LabelField($"Estimated DPS: {dps:F2}");

            string rarityColor = weapon.rarity switch
            {
                WeaponRarity.Common => "white",
                WeaponRarity.Uncommon => "green",
                WeaponRarity.Rare => "blue",
                WeaponRarity.Legendary => "orange",
                _ => "white"
            };
            EditorGUILayout.LabelField($"Rarity: <color={rarityColor}>{weapon.rarity}</color>", new GUIStyle(EditorStyles.label) { richText = true });

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(weapon);
            }
        }
    }
}
