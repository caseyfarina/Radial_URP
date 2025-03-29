#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FPSAndEntityCounter))]
public class FPSAndEntityCounterEditor : Editor
{
    SerializedProperty fpsDisplayProp;
    SerializedProperty entityCountDisplayProp;
    SerializedProperty fpsRefreshRateProp;
    SerializedProperty fpsFormatProp;
    SerializedProperty goodFpsColorProp;
    SerializedProperty warningFpsColorProp;
    SerializedProperty badFpsColorProp;
    SerializedProperty goodFpsThresholdProp;
    SerializedProperty warningFpsThresholdProp;
    SerializedProperty entityRefreshRateProp;
    SerializedProperty entityFormatProp;
    
    private void OnEnable()
    {
        // Find the serialized properties
        fpsDisplayProp = serializedObject.FindProperty("fpsDisplay");
        entityCountDisplayProp = serializedObject.FindProperty("entityCountDisplay");
        fpsRefreshRateProp = serializedObject.FindProperty("fpsRefreshRate");
        fpsFormatProp = serializedObject.FindProperty("fpsFormat");
        goodFpsColorProp = serializedObject.FindProperty("goodFpsColor");
        warningFpsColorProp = serializedObject.FindProperty("warningFpsColor");
        badFpsColorProp = serializedObject.FindProperty("badFpsColor");
        goodFpsThresholdProp = serializedObject.FindProperty("goodFpsThreshold");
        warningFpsThresholdProp = serializedObject.FindProperty("warningFpsThreshold");
        entityRefreshRateProp = serializedObject.FindProperty("entityRefreshRate");
        entityFormatProp = serializedObject.FindProperty("entityFormat");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Add a description header
        EditorGUILayout.HelpBox("Displays FPS and counts entities with the \"Entity\" tag.", MessageType.Info);
        
        // Display Settings Section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Display Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fpsDisplayProp);
        EditorGUILayout.PropertyField(entityCountDisplayProp);
        
        // FPS Settings Section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("FPS Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fpsRefreshRateProp);
        EditorGUILayout.PropertyField(fpsFormatProp);
        
        // Color Settings
        EditorGUILayout.LabelField("FPS Color Thresholds", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(goodFpsThresholdProp, new GUIContent("Good FPS >="));
        EditorGUILayout.PropertyField(goodFpsColorProp, GUIContent.none, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(warningFpsThresholdProp, new GUIContent("Warning FPS >="));
        EditorGUILayout.PropertyField(warningFpsColorProp, GUIContent.none, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Bad FPS <", GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(warningFpsThresholdProp.intValue.ToString());
        EditorGUILayout.PropertyField(badFpsColorProp, GUIContent.none, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        
        // Entity Counter Settings Section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Entity Counter Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(entityRefreshRateProp);
        EditorGUILayout.PropertyField(entityFormatProp);
        
        // Testing buttons in Play Mode
        EditorGUILayout.Space(10);
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Force Entity Count Refresh"))
            {
                FPSAndEntityCounter counter = (FPSAndEntityCounter)target;
                counter.ForceEntityCountRefresh();
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current FPS:", GUILayout.Width(100));
            EditorGUILayout.LabelField(((FPSAndEntityCounter)target).GetCurrentFPS().ToString());
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Entity Count:", GUILayout.Width(100));
            EditorGUILayout.LabelField(((FPSAndEntityCounter)target).GetEntityCount().ToString());
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see runtime values and test functionality.", MessageType.Info);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
