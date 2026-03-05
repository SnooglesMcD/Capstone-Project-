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
    
    [Header("Specific Path Sounds")]
    public List<PathSoundTrigger> pathSpecificSounds = new List<PathSoundTrigger>();

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
    private AudioSource audioSource;

    void Awake()
    {
        // Ensure we have an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning($"Added AudioSource to {gameObject.name} - please check its settings");
        }
        
        // Log AudioSource settings for debugging
        Debug.Log($"AudioSource on {gameObject.name}: Volume={audioSource.volume}, Mute={audioSource.mute}, Spatial={audioSource.spatialBlend}");
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
            if (hasKey)
            {
                Debug.Log("Door unlocked with key! Loading " + destination + "...");
                PlayDoorOpenSound();
                PlayAreaExitSoundWithPath();
                
                PlayerPrefs.SetString("LastDoorSourceArea", sourceAreaTag);
                PlayerPrefs.SetString("NextSceneEntranceSound", destination);
                PlayerPrefs.SetString("LastPathFrom", sourceAreaTag);
                PlayerPrefs.SetString("LastPathTo", destination);
                
                SceneManager.LoadScene(destination);
            }
            else
            {
                Debug.Log("Door is locked! Need key.");
                PlayLockedFeedback();
                
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(locked_dialogue_id);
                }
            }
        }
        else
        {
            if (isUnlocked || !startLocked)
            {
                Debug.Log("Door opened! Loading " + destination + "...");
                PlayDoorOpenSound();
                PlayAreaExitSoundWithPath();
                
                PlayerPrefs.SetString("LastDoorSourceArea", sourceAreaTag);
                PlayerPrefs.SetString("NextSceneEntranceSound", destination);
                PlayerPrefs.SetString("LastPathFrom", sourceAreaTag);
                PlayerPrefs.SetString("LastPathTo", destination);
                
                SceneManager.LoadScene(destination);
            }
            else
            {
                Debug.Log("Door is locked!");
                PlayLockedFeedback();
                
                if (DialogueManager.Instance != null && !string.IsNullOrEmpty(locked_dialogue_id))
                {
                    DialogueManager.Instance.StartDialogue(locked_dialogue_id);
                }
            }
        }
    }

    void PlayAreaExitSoundWithPath()
    {
        Debug.Log($"Checking exit sounds: From={sourceAreaTag}, To={destination}");
        
        // Check for path-specific exit sound first
        foreach (var pathTrigger in pathSpecificSounds)
        {
            Debug.Log($"Comparing with path: {pathTrigger.fromArea} → {pathTrigger.toArea}, HasSound={pathTrigger.exitSound != null}");
            
            if (pathTrigger.fromArea == sourceAreaTag && 
                pathTrigger.toArea == destination && 
                pathTrigger.exitSound != null &&
                pathTrigger.CanPlay())
            {
                Debug.Log($"✓ Found matching path-specific exit sound: {pathTrigger.fromArea} → {pathTrigger.toArea}");
                
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(pathTrigger.exitSound);
                    Debug.Log($"▶ Playing exit sound: {pathTrigger.exitSound.name}, Length: {pathTrigger.exitSound.length}sec");
                    pathTrigger.MarkPlayed();
                }
                else
                {
                    Debug.LogError("❌ AudioSource is null!");
                }
                return;
            }
        }
        
        // Fall back to generic area exit sound
        if (!string.IsNullOrEmpty(sourceAreaTag) && areaExitSound != null)
        {
            Debug.Log($"Playing generic exit sound for area: {sourceAreaTag}");
            if (audioSource != null)
            {
                audioSource.PlayOneShot(areaExitSound);
                Debug.Log($"▶ Playing generic exit sound: {areaExitSound.name}");
            }
        }
        else
        {
            Debug.Log("No exit sound found or area tag is empty");
        }
    }

    public void PlayAreaEntranceSound()
    {
        string lastArea = PlayerPrefs.GetString("LastDoorSourceArea", "");
        string lastFrom = PlayerPrefs.GetString("LastPathFrom", "");
        string lastTo = PlayerPrefs.GetString("LastPathTo", "");
        string expectedScene = PlayerPrefs.GetString("NextSceneEntranceSound", "");
        
        Debug.Log($"=== ENTRANCE SOUND CHECK ===");
        Debug.Log($"Door: {gameObject.name} in scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Last From: {lastFrom}, Last To: {lastTo}");
        Debug.Log($"Last Area: {lastArea}, Expected Scene: {expectedScene}");
        Debug.Log($"This door's source tag: {sourceAreaTag}");
        
        // Check for path-specific entrance sound first
        foreach (var pathTrigger in pathSpecificSounds)
        {
            Debug.Log($"Checking path: {pathTrigger.fromArea} → {pathTrigger.toArea}, HasEnterSound={pathTrigger.enterSound != null}");
            
            if (pathTrigger.fromArea == lastFrom && 
                pathTrigger.toArea == lastTo && 
                pathTrigger.enterSound != null &&
                pathTrigger.CanPlay())
            {
                Debug.Log($"✓ FOUND MATCHING PATH: {pathTrigger.fromArea} → {pathTrigger.toArea}");
                
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(pathTrigger.enterSound);
                    Debug.Log($"▶ PLAYING ALARM: {pathTrigger.enterSound.name}, Length: {pathTrigger.enterSound.length}sec");
                    pathTrigger.MarkPlayed();
                }
                else
                {
                    Debug.LogError("❌ AudioSource is null!");
                }
                
                // Clear the stored info
                PlayerPrefs.DeleteKey("LastDoorSourceArea");
                PlayerPrefs.DeleteKey("NextSceneEntranceSound");
                PlayerPrefs.DeleteKey("LastPathFrom");
                PlayerPrefs.DeleteKey("LastPathTo");
                return;
            }
        }
        
        // Fall back to generic area entrance sound
        if (expectedScene == SceneManager.GetActiveScene().name || 
            (!string.IsNullOrEmpty(sourceAreaTag) && sourceAreaTag == lastArea))
        {
            if (areaEnterSound != null)
            {
                Debug.Log($"Playing generic entrance sound for area: {sourceAreaTag}");
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(areaEnterSound);
                    Debug.Log($"▶ Playing generic entrance sound: {areaEnterSound.name}");
                }
            }
        }
        
        // Clear the stored info
        PlayerPrefs.DeleteKey("LastDoorSourceArea");
        PlayerPrefs.DeleteKey("NextSceneEntranceSound");
        PlayerPrefs.DeleteKey("LastPathFrom");
        PlayerPrefs.DeleteKey("LastPathTo");
    }

    void PlayDoorOpenSound()
    {
        if (audioSource != null && doorOpenSound != null)
        {
            audioSource.PlayOneShot(doorOpenSound);
            Debug.Log($"Playing door open sound: {doorOpenSound.name}");
        }
        else if (audioSource != null)
        {
            audioSource.Play();
            Debug.Log("Playing default audio");
        }
    }

    void PlayLockedFeedback()
    {
        if (audioSource != null)
        {
            if (doorLockedSound != null)
            {
                audioSource.PlayOneShot(doorLockedSound);
                Debug.Log($"Playing locked sound: {doorLockedSound.name}");
            }
            else
            {
                audioSource.Play();
                Debug.Log("Playing default locked sound");
            }
        }
    }

    void Start()
    {
        Debug.Log($"Door {gameObject.name} started in scene {SceneManager.GetActiveScene().name}");
        PlayAreaEntranceSound();
    }

    void OnDestroy()
    {
        PlayerPrefs.DeleteKey("LastDoorSourceArea");
        PlayerPrefs.DeleteKey("NextSceneEntranceSound");
        PlayerPrefs.DeleteKey("LastPathFrom");
        PlayerPrefs.DeleteKey("LastPathTo");
    }
}