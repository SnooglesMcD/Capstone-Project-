using UnityEngine;
using UnityEngine.SceneManagement;

public class door_lock_controller : MonoBehaviour
{
    public string required_key_id = "key";
    public string locked_dialogue_id = "basement_door_locked";
    public string destination;    
    public GameObject keyObject; // Assign the actual key GameObject in inspector
    public bool requireKey = true; // Toggle to switch between key-required and always-open modes
    public bool startLocked = true; // For non-key doors that start locked

    private bool hasKey = false;
    private bool isUnlocked = false; // For non-key door state

    // Call this method when player picks up the key
    public void KeyCollected()
    {
        hasKey = true;
        Debug.Log("Key collected for door!");
    }

    // Call this to unlock doors that don't use keys
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
                Debug.Log("Door unlocked with key! Loading foyer...");
                SceneManager.LoadScene(destination);
            }
            else
            {
                Debug.Log("Door is locked! Need key.");
                
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(locked_dialogue_id);
                }
                
                PlayLockedFeedback();
            }
        }
        else
        {
            // New behavior - doors that don't need keys
            if (isUnlocked || !startLocked)
            {
                Debug.Log("Door opened! Loading " + destination + "...");
                SceneManager.LoadScene(destination);
            }
            else
            {
                Debug.Log("Door is locked!");
                
                if (DialogueManager.Instance != null && !string.IsNullOrEmpty(locked_dialogue_id))
                {
                    DialogueManager.Instance.StartDialogue(locked_dialogue_id);
                }
                
                PlayLockedFeedback();
            }
        }
    }

    void PlayLockedFeedback()
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.Play();
        }
    }
}