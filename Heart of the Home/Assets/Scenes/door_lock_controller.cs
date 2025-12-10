using UnityEngine;
using UnityEngine.SceneManagement;

public class door_lock_controller : MonoBehaviour
{
    public string required_key_id = "key";
    public string locked_dialogue_id = "basement_door_locked";    
    public GameObject keyObject; // Assign the actual key GameObject in inspector

    private bool hasKey = false;

    // Call this method when player picks up the key
    public void KeyCollected()
    {
        hasKey = true;
        Debug.Log("Key collected for door!");
    }

    public void OnInteract()
    {
        if (hasKey)
        {
            Debug.Log("Door unlocked with key! Loading foyer...");
            SceneManager.LoadScene("Foyer");
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

    void PlayLockedFeedback()
    {
        
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.Play();
        }
    }
}