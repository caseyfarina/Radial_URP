#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonitorManager))]
public class MonitorManagerEditor : Editor
{
    private SerializedProperty previousMonitorKeyProp;
    private SerializedProperty nextMonitorKeyProp;
    private SerializedProperty debugModeProp;
    private SerializedProperty preserveAspectRatioProp;
    private SerializedProperty setFullscreenOnSwitchProp;
    private SerializedProperty setBorderlessOnSwitchProp;
    
    // Startup properties
    private SerializedProperty initializeOnStartupProp;
    private SerializedProperty startupMonitorIndexProp;
    private SerializedProperty setFullscreenOnStartupProp;
    private SerializedProperty setBorderlessOnStartupProp;
    
    // Events
    private SerializedProperty onMonitorChangedProp;

    private void OnEnable()
    {
        previousMonitorKeyProp = serializedObject.FindProperty("previousMonitorKey");
        nextMonitorKeyProp = serializedObject.FindProperty("nextMonitorKey");
        debugModeProp = serializedObject.FindProperty("debugMode");
        preserveAspectRatioProp = serializedObject.FindProperty("preserveAspectRatio");
        setFullscreenOnSwitchProp = serializedObject.FindProperty("setFullscreenOnSwitch");
        setBorderlessOnSwitchProp = serializedObject.FindProperty("setBorderlessOnSwitch");
        
        // Startup properties
        initializeOnStartupProp = serializedObject.FindProperty("initializeOnStartup");
        startupMonitorIndexProp = serializedObject.FindProperty("startupMonitorIndex");
        setFullscreenOnStartupProp = serializedObject.FindProperty("setFullscreenOnStartup");
        setBorderlessOnStartupProp = serializedObject.FindProperty("setBorderlessOnStartup");
        
        // Events
        onMonitorChangedProp = serializedObject.FindProperty("onMonitorChanged");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Monitor Switching Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(previousMonitorKeyProp, new GUIContent("Previous Monitor Key", "Key to move to the previous monitor"));
        EditorGUILayout.PropertyField(nextMonitorKeyProp, new GUIContent("Next Monitor Key", "Key to move to the next monitor"));
        EditorGUILayout.PropertyField(debugModeProp, new GUIContent("Debug Mode", "Show debug messages in the console"));
        EditorGUILayout.PropertyField(preserveAspectRatioProp, new GUIContent("Preserve Aspect Ratio", "Maintain aspect ratio when changing monitor"));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Window Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(setFullscreenOnSwitchProp, new GUIContent("Fullscreen On Switch", "Set window to fullscreen when switching monitors"));
        
        // Disable setBorderlessOnSwitch if fullscreen is enabled
        EditorGUI.BeginDisabledGroup(setFullscreenOnSwitchProp.boolValue);
        EditorGUILayout.PropertyField(setBorderlessOnSwitchProp, new GUIContent("Borderless On Switch", "Set window to borderless when switching monitors"));
        EditorGUI.EndDisabledGroup();
        
        if (setFullscreenOnSwitchProp.boolValue && setBorderlessOnSwitchProp.boolValue)
        {
            setBorderlessOnSwitchProp.boolValue = false;
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Startup Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(initializeOnStartupProp, new GUIContent("Initialize On Startup", "Initialize window position when game starts"));
        
        EditorGUI.BeginDisabledGroup(!initializeOnStartupProp.boolValue);
        EditorGUILayout.PropertyField(startupMonitorIndexProp, new GUIContent("Startup Monitor Index", "Monitor to use on startup (0 is primary)"));
        
        EditorGUILayout.PropertyField(setFullscreenOnStartupProp, new GUIContent("Fullscreen On Startup", "Set fullscreen on startup"));
        
        // Disable borderless option if fullscreen is enabled
        EditorGUI.BeginDisabledGroup(setFullscreenOnStartupProp.boolValue);
        EditorGUILayout.PropertyField(setBorderlessOnStartupProp, new GUIContent("Borderless On Startup", "Set borderless on startup"));
        EditorGUI.EndDisabledGroup();
        
        if (setFullscreenOnStartupProp.boolValue && setBorderlessOnStartupProp.boolValue)
        {
            setBorderlessOnStartupProp.boolValue = false;
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onMonitorChangedProp, new GUIContent("On Monitor Changed", "Event triggered when monitor changes (passes monitor index)"));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("This component only works in standalone builds. Monitor switching is not supported in the editor.", MessageType.Info);
        
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
