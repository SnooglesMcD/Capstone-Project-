using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class door_lock_controller : MonoBehaviour
{
    public string required_key_id = "key";
    public string locked_dialogue_id = "basement_door_locked";
    public string destination;    
    public GameObject keyObject;
    public bool requireKey = true;
    public bool startLocked = true;

    [Header("Sound Effects")]
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;
    public AudioClip doorLockedSound;
    public string sourceAreaTag = "";

    [Header("Area-Specific Sounds")]
    public AudioClip areaEnterSound;
    public AudioClip areaExitSound;
    
    [Header("Path-Specific Sounds")]
    public List<PathSoundTrigger> pathSpecificSounds = new List<PathSoundTrigger>();

    [Header("Audio Sources")]
    public AudioSource mainAudioSource;      // For door sounds
    public AudioSource alarmAudioSource;     // Dedicated for alarms
    public bool debugMode = true;

    [System.Serializable]
    public class PathSoundTrigger
    {
        public string fromArea;
        public string toArea;
        public AudioClip enterSound;
        public AudioClip exitSound;
        public bool oneTimeOnly = false;
        private bool hasPlayed = false;
        
        public bool CanPlay()
        {
            return !oneTimeOnly || !hasPlayed;
        }
        
        public void MarkPlayed()
        {
            hasPlayed = true;
        }
    }

    private bool hasKey = false;
    private bool isUnlocked = false;

    void Awake()
    {
        SetupAudioSources();
    }

    void SetupAudioSources()
    {
        // Ensure main audio source exists (for door sounds)
        if (mainAudioSource == null)
        {
            mainAudioSource = GetComponent<AudioSource>();
            
            // If still null, add one to this GameObject
            if (mainAudioSource == null)
            {
                mainAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log($"Added main AudioSource to {gameObject.name}");
            }
        }
        
        // Configure main audio source
        if (mainAudioSource != null)
        {
            mainAudioSource.playOnAwake = false;
            mainAudioSource.spatialBlend = 0.5f; // Slight 3D effect for door sounds
            
            if (debugMode)
            {
                Debug.Log($"Main AudioSource on {gameObject.name}: Volume={mainAudioSource.volume}, " +
                          $"Mute={mainAudioSource.mute}, Spatial={mainAudioSource.spatialBlend}");
            }
        }
        
        // Ensure alarm audio source exists
        if (alarmAudioSource == null)
        {
            // Try to find in children first
            alarmAudioSource = GetComponentInChildren<AudioSource>();
            
            // If still null, create a child GameObject with AudioSource
            if (alarmAudioSource == null)
            {
                GameObject alarmObj = new GameObject("AlarmAudioSource");
                alarmObj.transform.parent = transform;
                alarmObj.transform.localPosition = Vector3.zero;
                alarmAudioSource = alarmObj.AddComponent<AudioSource>();
                if (debugMode) Debug.Log($"Created AlarmAudioSource on {gameObject.name}");
            }
        }
        
        // Configure alarm source for 2D sound
        if (alarmAudioSource != null)
        {
            alarmAudioSource.spatialBlend = 0f; // 2D = global sound
            alarmAudioSource.volume = 1f;
            alarmAudioSource.playOnAwake = false;
            
            if (debugMode)
            {
                Debug.Log($"Alarm AudioSource on {gameObject.name}: Volume={alarmAudioSource.volume}, " +
                          $"Mute={alarmAudioSource.mute}, Spatial={alarmAudioSource.spatialBlend}");
            }
        }
    }

    public void KeyCollected()
    {
        hasKey = true;
        Debug.Log("Key collected for door!");
    }

    public void UnlockDoor()
    {
        isUnlocked = true;
        Debug.Log("Door unlocked!");
    }

    public void OnInteract()
    {
        if (requireKey)
        {
            // Original key-required behavior
            if (hasKey)
            {
                Debug.Log("Door unlocked with key! Loading " + destination + "...");
                PlaySound(mainAudioSource, doorOpenSound, "door open");
                PlayAreaExitSoundWithPath();
                
                // Save path information BEFORE loading scene
                PlayerPrefs.SetString("LastPathFrom", sourceAreaTag);
                PlayerPrefs.SetString("LastPathTo", destination);
                if (debugMode) Debug.Log($"Saved path: {sourceAreaTag} → {destination}");
                
                SceneManager.LoadScene(destination);
            }
            else
            {
                Debug.Log("Door is locked! Need key.");
                PlaySound(mainAudioSource, doorLockedSound, "locked");
                
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(locked_dialogue_id);
                }
            }
        }
        else
        {
            // New behavior - doors that don't need keys
            if (isUnlocked || !startLocked)
            {
                Debug.Log("Door opened! Loading " + destination + "...");
                PlaySound(mainAudioSource, doorOpenSound, "door open");
                PlayAreaExitSoundWithPath();
                
                // Save path information BEFORE loading scene
                PlayerPrefs.SetString("LastPathFrom", sourceAreaTag);
                PlayerPrefs.SetString("LastPathTo", destination);
                if (debugMode) Debug.Log($"Saved path: {sourceAreaTag} → {destination}");
                
                SceneManager.LoadScene(destination);
            }
            else
            {
                Debug.Log("Door is locked!");
                PlaySound(mainAudioSource, doorLockedSound, "locked");
                
                if (DialogueManager.Instance != null && !string.IsNullOrEmpty(locked_dialogue_id))
                {
                    DialogueManager.Instance.StartDialogue(locked_dialogue_id);
                }
            }
        }
    }

    void PlayAreaExitSoundWithPath()
    {
        if (debugMode) Debug.Log($"Checking exit sounds: From={sourceAreaTag}, To={destination}");
        
        // Check for path-specific exit sound first
        foreach (var pathTrigger in pathSpecificSounds)
        {
            bool hasSound = pathTrigger.exitSound != null;
            if (debugMode) Debug.Log($"Comparing with path: {pathTrigger.fromArea} → {pathTrigger.toArea}, HasSound={hasSound}");
            
            if (pathTrigger.fromArea == sourceAreaTag && 
                pathTrigger.toArea == destination && 
                pathTrigger.exitSound != null &&
                pathTrigger.CanPlay())
            {
                PlaySound(alarmAudioSource, pathTrigger.exitSound, $"path exit: {pathTrigger.fromArea}→{pathTrigger.toArea}");
                pathTrigger.MarkPlayed();
                return;
            }
        }
        
        // Fall back to generic area exit sound
        if (!string.IsNullOrEmpty(sourceAreaTag) && areaExitSound != null)
        {
            PlaySound(alarmAudioSource, areaExitSound, $"generic exit: {sourceAreaTag}");
        }
        else if (debugMode)
        {
            Debug.Log("No exit sound found or area tag is empty");
        }
    }

    public void PlayAreaEntranceSound()
    {
        string lastFrom = PlayerPrefs.GetString("LastPathFrom", "");
        string lastTo = PlayerPrefs.GetString("LastPathTo", "");
        
        // If no path data, exit early - don't clear anything
        if (string.IsNullOrEmpty(lastFrom) || string.IsNullOrEmpty(lastTo))
        {
            if (debugMode) Debug.Log($"No path data for {gameObject.name} in scene {SceneManager.GetActiveScene().name}");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"=== ENTRANCE SOUND CHECK on {gameObject.name} ===");
            Debug.Log($"Scene: {SceneManager.GetActiveScene().name}");
            Debug.Log($"This door's source tag: {sourceAreaTag}");
            Debug.Log($"This door's destination: {destination}");
            Debug.Log($"Last path from PlayerPrefs: {lastFrom} → {lastTo}");
            Debug.Log($"PathSpecificSounds count: {pathSpecificSounds.Count}");
        }
        
        // Check for path-specific entrance sound
        foreach (var pathTrigger in pathSpecificSounds)
        {
            bool fromMatch = pathTrigger.fromArea == lastFrom;
            bool toMatch = pathTrigger.toArea == lastTo;
            bool hasSound = pathTrigger.enterSound != null;
            
            if (debugMode)
                Debug.Log($"Checking path: {pathTrigger.fromArea} → {pathTrigger.toArea}, " +
                         $"FromMatch: {fromMatch}, ToMatch: {toMatch}, HasEnterSound={hasSound}");
            
            if (fromMatch && toMatch && pathTrigger.enterSound != null && pathTrigger.CanPlay())
            {
                Debug.Log($"✓ FOUND MATCH on {gameObject.name} in scene {SceneManager.GetActiveScene().name}");
                Debug.Log($"Playing alarm: {pathTrigger.enterSound.name}");
                
                // Play the sound using alarm source (with fallback)
                PlaySound(alarmAudioSource, pathTrigger.enterSound, $"ALARM: {pathTrigger.fromArea}→{pathTrigger.toArea}");
                
                // ONLY clear AFTER successfully playing
                PlayerPrefs.DeleteKey("LastPathFrom");
                PlayerPrefs.DeleteKey("LastPathTo");
                Debug.Log("Cleared path data after playing sound");
                
                pathTrigger.MarkPlayed();
                return;
            }
        }
        
        // Check for generic area entrance sound as fallback
        string expectedScene = PlayerPrefs.GetString("NextSceneEntranceSound", "");
        if (expectedScene == SceneManager.GetActiveScene().name || 
            (!string.IsNullOrEmpty(sourceAreaTag) && sourceAreaTag == lastFrom))
        {
            if (areaEnterSound != null)
            {
                Debug.Log($"Playing generic entrance sound for area: {sourceAreaTag}");
                PlaySound(alarmAudioSource, areaEnterSound, "generic entrance");
                PlayerPrefs.DeleteKey("LastPathFrom");
                PlayerPrefs.DeleteKey("LastPathTo");
                PlayerPrefs.DeleteKey("NextSceneEntranceSound");
                return;
            }
        }
        
        if (debugMode) Debug.Log($"No matching path on {gameObject.name}");
        // DON'T clear here - let other doors in the scene check
    }

    void PlaySound(AudioSource source, AudioClip clip, string soundName)
    {
        // If the specified source is null, try to find a valid one
        if (source == null)
        {
            if (debugMode) Debug.LogWarning($"Specified AudioSource is null for {soundName} on {gameObject.name}, trying to find one...");
            
            // Try mainAudioSource first
            if (mainAudioSource != null)
            {
                source = mainAudioSource;
                if (debugMode) Debug.Log($"Using mainAudioSource for {soundName}");
            }
            // Then try alarmAudioSource
            else if (alarmAudioSource != null)
            {
                source = alarmAudioSource;
                if (debugMode) Debug.Log($"Using alarmAudioSource for {soundName}");
            }
            // If both are null, try to get or add one
            else
            {
                source = GetComponent<AudioSource>();
                if (source == null)
                {
                    source = gameObject.AddComponent<AudioSource>();
                    if (debugMode) Debug.Log($"Added new AudioSource to {gameObject.name} for {soundName}");
                }
                
                // Configure the new source
                source.playOnAwake = false;
                source.spatialBlend = 0.5f;
            }
        }
        
        if (clip == null)
        {
            if (debugMode) Debug.LogWarning($"AudioClip is null for {soundName} on {gameObject.name}");
            return;
        }
        
        if (debugMode) Debug.Log($"▶ Playing {soundName}: {clip.name} on {source.gameObject.name}");
        source.PlayOneShot(clip);
    }

    void PlayDoorOpenSound()
    {
        PlaySound(mainAudioSource, doorOpenSound, "door open");
    }

    void PlayLockedFeedback()
    {
        PlaySound(mainAudioSource, doorLockedSound, "locked");
    }

    void Start()
    {
        if (debugMode) Debug.Log($"Door {gameObject.name} started in scene {SceneManager.GetActiveScene().name}");
        PlayAreaEntranceSound();
    }

    void OnDestroy()
    {
        // Don't clear anything here - let PlayAreaEntranceSound() handle it
        // This prevents clearing data before the destination scene can read it
    }
}