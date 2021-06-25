using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorInspector : Editor {
    
    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("TileWorldSize"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MapTileWidth"), true);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ThickFloorPrefab"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ThinFloorPrefab"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PillarPrefab"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AqueductPrefab1Way"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AqueductPrefab2WayStraight"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AqueductPrefab2WayCorner"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AqueductPrefab3Way"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AqueductPrefab4Way"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SmallBuildingPrefabs"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LargeBuildingPrefabs"), true);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("FloorParentTransform"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AqueductParentTransform"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PillarParentTransform"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("BuildingParentTransform"), true);
            
        serializedObject.ApplyModifiedProperties();
            
        LevelGenerator generator = (LevelGenerator)target;
        if (GUILayout.Button("Destroy Current")) {
            generator.DestroyOldTerrain();
        }
        if (GUILayout.Button("Generate")) {
            generator.Generate();
        }
    }
}
