using UnityEngine;
using UnityEngine.Audio;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

public enum PlaybackMode
{
    RandomEachTime,
    PreselectedRandom
}

[RequireComponent(typeof(AudioSource))]
public class RandomAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string selectedFolder = "sauron";  // Default folder

    [Header("Playback Mode")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.RandomEachTime;

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1.0f;
    [SerializeField, Range(0f, 100f)] private float pitchVariancePercent;

    [Header("Mixer Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    private List<AudioClip> audioClips = new();
    private AudioClip preselectedClip;

    private void Awake()
    {
        InitializeAudioSource();
        LoadAudioClips();

        if (playbackMode == PlaybackMode.PreselectedRandom)
        {
            SelectRandomClip();
        }
    }

    private void OnValidate()
    {
        if (minVolume > maxVolume)
        {
            minVolume = maxVolume;
        }
    }

    private void InitializeAudioSource()
    {
        audioSource = audioSource ?? GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure default settings
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;  // Default to 2D sound
        audioSource.loop = false;

        if (outputMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = outputMixerGroup;
        }
    }

    public void LoadAudioClips()
    {
        if (string.IsNullOrEmpty(selectedFolder))
        {
            Debug.LogError("[RandomAudioPlayer] No folder selected!");
            return;
        }

        audioClips.Clear();
        Debug.Log($"[RandomAudioPlayer] Attempting to load audio clips from folder: {selectedFolder}");

#if UNITY_EDITOR
        LoadAudioClipsInEditor();
#else
        LoadAudioClipsAtRuntime();
#endif

        LogLoadingResults();

        // If we're in preselected mode, select a random clip after loading
        if (playbackMode == PlaybackMode.PreselectedRandom && audioClips.Count > 0)
        {
            SelectRandomClip();
        }
    }

    public void SelectRandomClip()
    {
        if (audioClips.Count > 0)
        {
            int randomIndex = Random.Range(0, audioClips.Count);
            preselectedClip = audioClips[randomIndex];
            Debug.Log($"[RandomAudioPlayer] Preselected clip: {preselectedClip.name}");
        }
        else
        {
            preselectedClip = null;
            Debug.Log("[RandomAudioPlayer] Cannot preselect a clip: no clips loaded");
        }
    }

#if UNITY_EDITOR
    private void LoadAudioClipsInEditor()
    {
        string folderPath = Path.Combine("Assets", "Resources", "Samples", selectedFolder);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"[RandomAudioPlayer] Directory does not exist: {folderPath}");
            return;
        }

        string[] audioFiles = Directory.GetFiles(folderPath, "*.*")
            .Where(file => file.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase) ||
                          file.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase) ||
                          file.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase) ||
                          file.EndsWith(".aiff", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (string audioFile in audioFiles)
        {
            string assetPath = audioFile.Replace('\\', '/');
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
            {
                audioClips.Add(clip);
                Debug.Log($"[RandomAudioPlayer] Loaded clip: {clip.name}");
            }
            else
            {
                Debug.LogWarning($"[RandomAudioPlayer] Failed to load clip at path: {assetPath}");
            }
        }
    }
#endif

    private void LoadAudioClipsAtRuntime()
    {
        string resourcePath = "Samples/" + selectedFolder;
        AudioClip[] clips = Resources.LoadAll<AudioClip>(resourcePath);

        if (clips != null && clips.Length > 0)
        {
            audioClips.AddRange(clips);
            Debug.Log($"[RandomAudioPlayer] Loaded {clips.Length} clips from Resources/{resourcePath}");
        }
        else
        {
            Debug.LogError($"[RandomAudioPlayer] No clips found at Resources/{resourcePath}");
        }
    }

    private void LogLoadingResults()
    {
        if (audioClips.Count == 0)
        {
            Debug.LogError($"[RandomAudioPlayer] No audio clips found in folder: {selectedFolder}");
        }
        else
        {
            Debug.Log($"[RandomAudioPlayer] Successfully loaded {audioClips.Count} total audio clips");
        }
    }

    public void PlayRandomSound()
    {
        if (!CanPlaySound()) return;

        float randomVolume = Random.Range(minVolume, maxVolume);
        AudioClip clipToPlay;

        // Determine which clip to play based on mode
        if (playbackMode == PlaybackMode.PreselectedRandom)
        {
            // Use the preselected clip
            clipToPlay = preselectedClip;
            if (clipToPlay == null)
            {
                // If somehow preselectedClip is null, try to select one now
                SelectRandomClip();
                clipToPlay = preselectedClip;

                // If still null, exit
                if (clipToPlay == null) return;
            }
        }
        else
        {
            // RandomEachTime mode - choose a random clip
            int randomIndex = Random.Range(0, audioClips.Count);
            clipToPlay = audioClips[randomIndex];
        }

        if (pitchVariancePercent > 0)
        {
            // If pitch variation is needed, use a separate AudioSource
            PlaySoundWithPitch(clipToPlay, randomVolume);
        }
        else
        {
            // If no pitch variation, use PlayOneShot
            audioSource.PlayOneShot(clipToPlay, randomVolume);
        }
    }

    private void PlaySoundWithPitch(AudioClip clip, float volume)
    {
        // Create temporary AudioSource only when pitch variation is needed
        GameObject tempGO = new("TempAudio");
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();

        // Copy settings from main AudioSource
        tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        tempSource.spatialBlend = audioSource.spatialBlend;

        // Calculate pitch
        float pitchVarianceAmount = Mathf.Clamp(pitchVariancePercent, 0f, 100f) / 100f;
        float randomPitch = Random.Range(1f - pitchVarianceAmount, 1f + pitchVarianceAmount);
        tempSource.pitch = randomPitch;

        // Play sound
        tempSource.PlayOneShot(clip, volume);

        // Destroy after playing
        float clipLength = clip.length / randomPitch;
        Destroy(tempGO, clipLength + 0.1f);
    }

    private bool CanPlaySound()
    {
        if (audioClips.Count == 0)
        {
            Debug.Log("[RandomAudioPlayer] No audio clips loaded!");
            return false;
        }
        return true;
    }

    // Method to set playback mode from outside the class
    public void SetPlaybackMode(PlaybackMode mode)
    {
        playbackMode = mode;

        // If switching to preselected mode, select a clip right away
        if (mode == PlaybackMode.PreselectedRandom)
        {
            SelectRandomClip();
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RandomAudioPlayer))]
public class RandomAudioPlayerEditor : Editor
{
    private SerializedProperty audioSourceProp;
    private SerializedProperty selectedFolderProp;
    private SerializedProperty playbackModeProp;
    private SerializedProperty minVolumeProp;
    private SerializedProperty maxVolumeProp;
    private SerializedProperty pitchVarianceProp;
    private SerializedProperty audioMixerProp;
    private SerializedProperty outputMixerGroupProp;
    private string[] folderOptions;

    private void OnEnable()
    {
        InitializeProperties();
        UpdateFolderList();
    }

    private void InitializeProperties()
    {
        audioSourceProp = serializedObject.FindProperty("audioSource");
        selectedFolderProp = serializedObject.FindProperty("selectedFolder");
        playbackModeProp = serializedObject.FindProperty("playbackMode");
        minVolumeProp = serializedObject.FindProperty("minVolume");
        maxVolumeProp = serializedObject.FindProperty("maxVolume");
        pitchVarianceProp = serializedObject.FindProperty("pitchVariancePercent");
        audioMixerProp = serializedObject.FindProperty("audioMixer");
        outputMixerGroupProp = serializedObject.FindProperty("outputMixerGroup");
    }

    private void UpdateFolderList()
    {
        string samplesPath = Path.Combine(Application.dataPath, "Resources", "Samples");

        if (!Directory.Exists(samplesPath))
        {
            Directory.CreateDirectory(samplesPath);
        }

        folderOptions = Directory.GetDirectories(samplesPath)
            .Select(Path.GetFileName)
            .ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(audioSourceProp);

        // Playback Mode dropdown
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(playbackModeProp, new GUIContent("Playback Mode"));
        bool playbackModeChanged = EditorGUI.EndChangeCheck();

        DrawFolderSelection();
        DrawAudioSettings();
        DrawMixerSettings();
        DrawTestControls();

        serializedObject.ApplyModifiedProperties();

        // If playback mode was changed and we're in play mode, update the selection
        if (playbackModeChanged && Application.isPlaying)
        {
            RandomAudioPlayer player = (RandomAudioPlayer)target;
            if (playbackModeProp.enumValueIndex == (int)PlaybackMode.PreselectedRandom)
            {
                player.SelectRandomClip();
            }
        }
    }

    private void DrawFolderSelection()
    {
        if (folderOptions == null || folderOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("No subfolders found in the Resources/Samples folder.", MessageType.Warning);
            return;
        }

        // Initialize with default folder if none selected
        if (string.IsNullOrEmpty(selectedFolderProp.stringValue))
        {
            selectedFolderProp.stringValue = folderOptions[0];
        }

        int folderIndex = System.Array.IndexOf(folderOptions, selectedFolderProp.stringValue);
        if (folderIndex == -1)
        {
            folderIndex = 0;
            selectedFolderProp.stringValue = folderOptions[folderIndex];
        }

        EditorGUI.BeginChangeCheck();
        int newFolderIndex = EditorGUILayout.Popup("Sample Folder", folderIndex, folderOptions);
        if (EditorGUI.EndChangeCheck())
        {
            selectedFolderProp.stringValue = folderOptions[newFolderIndex];
            RandomAudioPlayer audioPlayer = (RandomAudioPlayer)target;
            audioPlayer.LoadAudioClips();
        }

        EditorGUILayout.LabelField("Current Folder:", selectedFolderProp.stringValue);
    }

    private void DrawAudioSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);

        // Volume Range
        EditorGUILayout.LabelField("Volume Range");
        EditorGUILayout.BeginHorizontal();
        float minVol = minVolumeProp.floatValue;
        float maxVol = maxVolumeProp.floatValue;

        minVol = EditorGUILayout.Slider("Min", minVol, 0f, 1f);
        maxVol = EditorGUILayout.Slider("Max", maxVol, 0f, 1f);

        // Ensure min doesn't exceed max
        if (minVol > maxVol) minVol = maxVol;

        minVolumeProp.floatValue = minVol;
        maxVolumeProp.floatValue = maxVol;
        EditorGUILayout.EndHorizontal();

        // Pitch Variance
        EditorGUILayout.PropertyField(pitchVarianceProp, new GUIContent("Pitch Variance %"));
    }

    private void DrawMixerSettings()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mixer Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(audioMixerProp);
        EditorGUILayout.PropertyField(outputMixerGroupProp);
    }

    private void DrawTestControls()
    {
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Refresh Folder List"))
        {
            UpdateFolderList();
        }

        RandomAudioPlayer audioPlayer = (RandomAudioPlayer)target;

        using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
        {
            // Add button to reselect a random clip in preselected mode
            if (playbackModeProp.enumValueIndex == (int)PlaybackMode.PreselectedRandom)
            {
                if (GUILayout.Button("Select New Random Clip", GUILayout.Height(25)))
                {
                    audioPlayer.SelectRandomClip();
                }
            }

            if (GUILayout.Button("Play Test Sound", GUILayout.Height(30)))
            {
                audioPlayer.PlayRandomSound();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test sounds.", MessageType.Info);
            }
        }
    }
}
#endif