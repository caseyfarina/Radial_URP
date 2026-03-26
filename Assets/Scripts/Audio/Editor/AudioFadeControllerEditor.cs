using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

/// <summary>
/// Custom inspector for AudioFadeController that provides dropdown selection
/// for post-processing effects and parameters.
/// </summary>
[CustomEditor(typeof(AudioFadeController))]
public class AudioFadeControllerEditor : Editor
{
    // Property references
    private SerializedProperty smoothingStrengthProp;
    private SerializedProperty minVolumeThresholdProp;
    private SerializedProperty playOnAwakeProp;
    private SerializedProperty maxVolumeProp;
    private SerializedProperty volumeCurveProp;
    private SerializedProperty mapToPostProcessingProp;
    private SerializedProperty postProcessingVolumeProp;
    private SerializedProperty selectedEffectTypeProp;
    private SerializedProperty selectedParameterNameProp;
    private SerializedProperty postProcessingCurveProp;
    private SerializedProperty minParameterValueProp;
    private SerializedProperty maxParameterValueProp;
    private SerializedProperty invertParameterMappingProp;
    private SerializedProperty debugLoggingProp;

    // State for dropdowns
    private List<string> availableEffectTypes = new List<string>();
    private List<string> availableParameters = new List<string>();
    private int selectedEffectIndex = -1;
    private int selectedParameterIndex = -1;
    private bool effectListDirty = true;

    private void OnEnable()
    {
        smoothingStrengthProp = serializedObject.FindProperty("smoothingStrength");
        minVolumeThresholdProp = serializedObject.FindProperty("minVolumeThreshold");
        playOnAwakeProp = serializedObject.FindProperty("playOnAwake");
        maxVolumeProp = serializedObject.FindProperty("maxVolume");
        volumeCurveProp = serializedObject.FindProperty("volumeCurve");
        mapToPostProcessingProp = serializedObject.FindProperty("mapToPostProcessing");
        postProcessingVolumeProp = serializedObject.FindProperty("postProcessingVolume");
        selectedEffectTypeProp = serializedObject.FindProperty("selectedEffectType");
        selectedParameterNameProp = serializedObject.FindProperty("selectedParameterName");
        postProcessingCurveProp = serializedObject.FindProperty("postProcessingCurve");
        minParameterValueProp = serializedObject.FindProperty("minParameterValue");
        maxParameterValueProp = serializedObject.FindProperty("maxParameterValue");
        invertParameterMappingProp = serializedObject.FindProperty("invertParameterMapping");
        debugLoggingProp = serializedObject.FindProperty("debugLogging");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Audio Fade Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(smoothingStrengthProp);
        EditorGUILayout.PropertyField(minVolumeThresholdProp);
        EditorGUILayout.PropertyField(playOnAwakeProp);
        EditorGUILayout.PropertyField(maxVolumeProp);
        EditorGUILayout.PropertyField(volumeCurveProp);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Post-Processing Mapping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mapToPostProcessingProp);

        if (mapToPostProcessingProp.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(postProcessingVolumeProp);
            if (EditorGUI.EndChangeCheck())
            {
                effectListDirty = true;
            }

            Volume volume = (Volume)postProcessingVolumeProp.objectReferenceValue;

            if (volume != null && volume.profile != null)
            {
                if (effectListDirty)
                {
                    RebuildEffectsList(volume.profile);
                }

                EditorGUI.BeginChangeCheck();
                selectedEffectIndex = availableEffectTypes.IndexOf(selectedEffectTypeProp.stringValue);
                string[] effectNames = new string[availableEffectTypes.Count];
                for (int i = 0; i < availableEffectTypes.Count; i++)
                {
                    Type type = Type.GetType(availableEffectTypes[i]);
                    effectNames[i] = type != null ? type.Name : availableEffectTypes[i];
                }

                selectedEffectIndex = EditorGUILayout.Popup("Effect Type", selectedEffectIndex, effectNames);
                if (EditorGUI.EndChangeCheck() && selectedEffectIndex >= 0 && selectedEffectIndex < availableEffectTypes.Count)
                {
                    selectedEffectTypeProp.stringValue = availableEffectTypes[selectedEffectIndex];
                    RebuildParametersList(volume.profile, availableEffectTypes[selectedEffectIndex]);
                    selectedParameterNameProp.stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                }

                if (selectedEffectIndex >= 0)
                {
                    if (availableParameters.Count == 0)
                    {
                        RebuildParametersList(volume.profile, selectedEffectTypeProp.stringValue);
                    }

                    if (availableParameters.Count > 0)
                    {
                        EditorGUI.BeginChangeCheck();
                        selectedParameterIndex = availableParameters.IndexOf(selectedParameterNameProp.stringValue);
                        if (selectedParameterIndex < 0) selectedParameterIndex = 0;
                        selectedParameterIndex = EditorGUILayout.Popup("Parameter", selectedParameterIndex, availableParameters.ToArray());
                        if (EditorGUI.EndChangeCheck() && selectedParameterIndex >= 0 && selectedParameterIndex < availableParameters.Count)
                        {
                            selectedParameterNameProp.stringValue = availableParameters[selectedParameterIndex];
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No compatible parameters found in this effect", MessageType.Warning);
                    }
                }

                EditorGUILayout.PropertyField(minParameterValueProp, new GUIContent("Min Value"));
                EditorGUILayout.PropertyField(maxParameterValueProp, new GUIContent("Max Value"));
                EditorGUILayout.PropertyField(invertParameterMappingProp, new GUIContent("Invert Mapping"));
                EditorGUILayout.PropertyField(postProcessingCurveProp, new GUIContent("Parameter Curve"));
            }
            else
            {
                EditorGUILayout.HelpBox("Please assign a Volume with a valid profile", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(debugLoggingProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void RebuildEffectsList(VolumeProfile profile)
    {
        availableEffectTypes.Clear();

        if (profile == null) return;

        var components = profile.components;

        foreach (var component in components)
        {
            if (component != null)
            {
                availableEffectTypes.Add(component.GetType().AssemblyQualifiedName);
            }
        }

        effectListDirty = false;
    }

    private void RebuildParametersList(VolumeProfile profile, string effectTypeName)
    {
        availableParameters.Clear();

        if (profile == null || string.IsNullOrEmpty(effectTypeName)) return;

        Type effectType = Type.GetType(effectTypeName);
        if (effectType == null) return;

        object effectComponent = null;
        foreach (var component in profile.components)
        {
            if (component.GetType().AssemblyQualifiedName == effectTypeName)
            {
                effectComponent = component;
                break;
            }
        }

        if (effectComponent != null)
        {
            FieldInfo[] fields = effectComponent.GetType().GetFields(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.Name == "active" ||
                    field.Name == "overrideState" ||
                    field.Name == "hideFlags")
                    continue;

                object fieldValue = field.GetValue(effectComponent);
                if (fieldValue != null)
                {
                    Type fieldType = fieldValue.GetType();

                    if (fieldType.Name.Contains("Parameter") ||
                        fieldType.Name.Contains("MinFloat") ||
                        fieldType.Name.Contains("MaxFloat") ||
                        fieldType.Name.Contains("ClampedFloat") ||
                        fieldType.Name.Contains("ClampedInt"))
                    {
                        var valueField = fieldType.GetField("value");
                        var valueProperty = fieldType.GetProperty("value");

                        if (valueField != null || valueProperty != null)
                        {
                            availableParameters.Add(field.Name);
                        }
                    }
                }
            }

            if (availableParameters.Count == 0)
            {
                PropertyInfo[] properties = effectComponent.GetType().GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.Name == "name" || property.Name == "active" ||
                        property.Name == "displayName" || property.Name == "observer" ||
                        property.Name == "hideFlags")
                        continue;

                    if (!property.CanWrite) continue;

                    if (property.PropertyType.Name.Contains("Parameter"))
                    {
                        object paramValue = property.GetValue(effectComponent);
                        if (paramValue != null)
                        {
                            Type paramType = paramValue.GetType();
                            var valueField = paramType.GetField("value");
                            var valueProperty = paramType.GetProperty("value");

                            if (valueField != null || valueProperty != null)
                            {
                                availableParameters.Add(property.Name);
                            }
                        }
                    }
                }
            }
        }
    }
}
