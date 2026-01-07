using UnityEngine;
using UnityEditor;
using VoidBreaker.Data;

namespace VoidBreaker.Editor
{
    [CustomEditor(typeof(ShipDefinition))]
    public class ShipDefinitionEditor : UnityEditor.Editor
    {
        private bool showRooms = true;
        private bool showSystems = true;
        private bool showWeapons = true;
        private bool showCrew = true;

        public override void OnInspectorGUI()
        {
            ShipDefinition ship = (ShipDefinition)target;

            // Header with ship preview
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Ship Definition", EditorStyles.boldLabel);

            if (ship.shipSprite != null)
            {
                GUILayout.Label(ship.shipSprite.texture, GUILayout.Height(100));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            ship.shipId = EditorGUILayout.TextField("Ship ID", ship.shipId);
            ship.shipName = EditorGUILayout.TextField("Ship Name", ship.shipName);
            ship.shipClass = (ShipClass)EditorGUILayout.EnumPopup("Ship Class", ship.shipClass);

            EditorGUILayout.LabelField("Description");
            ship.description = EditorGUILayout.TextArea(ship.description, GUILayout.Height(60));

            EditorGUILayout.Space();

            // Stats
            EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);
            ship.maxHull = EditorGUILayout.IntSlider("Max Hull", ship.maxHull, 10, 50);
            ship.maxShieldLayers = EditorGUILayout.IntSlider("Max Shield Layers", ship.maxShieldLayers, 0, 8);
            ship.reactorPower = EditorGUILayout.IntSlider("Reactor Power", ship.reactorPower, 4, 30);
            ship.baseEvasion = EditorGUILayout.IntSlider("Base Evasion", ship.baseEvasion, 0, 40);
            ship.weaponSlots = EditorGUILayout.IntSlider("Weapon Slots", ship.weaponSlots, 1, 6);
            ship.droneSlots = EditorGUILayout.IntSlider("Drone Slots", ship.droneSlots, 0, 4);

            EditorGUILayout.Space();

            // Rooms
            showRooms = EditorGUILayout.Foldout(showRooms, "Room Layout");
            if (showRooms)
            {
                EditorGUI.indentLevel++;
                SerializedProperty roomsProp = serializedObject.FindProperty("rooms");
                EditorGUILayout.PropertyField(roomsProp, true);
                EditorGUI.indentLevel--;
            }

            // Systems
            showSystems = EditorGUILayout.Foldout(showSystems, "Starting Systems");
            if (showSystems)
            {
                EditorGUI.indentLevel++;
                SerializedProperty systemsProp = serializedObject.FindProperty("startingSystems");
                EditorGUILayout.PropertyField(systemsProp, true);
                EditorGUI.indentLevel--;
            }

            // Weapons
            showWeapons = EditorGUILayout.Foldout(showWeapons, "Starting Weapons");
            if (showWeapons)
            {
                EditorGUI.indentLevel++;
                SerializedProperty weaponsProp = serializedObject.FindProperty("startingWeapons");
                EditorGUILayout.PropertyField(weaponsProp, true);
                EditorGUI.indentLevel--;
            }

            // Crew
            showCrew = EditorGUILayout.Foldout(showCrew, "Starting Crew");
            if (showCrew)
            {
                EditorGUI.indentLevel++;
                SerializedProperty crewProp = serializedObject.FindProperty("startingCrew");
                EditorGUILayout.PropertyField(crewProp, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Unlock
            EditorGUILayout.LabelField("Unlock Requirements", EditorStyles.boldLabel);
            ship.isUnlockedByDefault = EditorGUILayout.Toggle("Unlocked By Default", ship.isUnlockedByDefault);
            if (!ship.isUnlockedByDefault)
            {
                ship.unlockAchievementId = EditorGUILayout.TextField("Unlock Achievement", ship.unlockAchievementId);
            }

            // Visuals
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            ship.shipSprite = (Sprite)EditorGUILayout.ObjectField("Ship Sprite", ship.shipSprite, typeof(Sprite), false);
            ship.shipIcon = (Sprite)EditorGUILayout.ObjectField("Ship Icon", ship.shipIcon, typeof(Sprite), false);
            ship.baseColor = EditorGUILayout.ColorField("Base Color", ship.baseColor);

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(ship);
            }
        }
    }
}
