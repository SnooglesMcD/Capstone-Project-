using UnityEngine;

public class Toybox_controller : MonoBehaviour
{
    [Header("Dialogue")]
    public string dialogueId = "toybox_interact";
    
    
    [Header("Feedback")]
    public AudioClip interactionSound;
    
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Ensure collider is enabled
        GetComponent<Collider>().enabled = true;
        
        // Set up audio
        if (interactionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    public void OnInteract()
    {
        
        // Play dialogue
        if (DialogueManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(dialogueId))
            {
            DialogueManager.Instance.StartDialogue(dialogueId);
            }
        }
        
        // Play sound
        if (audioSource != null && interactionSound != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
        
        Debug.Log($"Toy box interacted with. Played dialogue: {dialogueId}");
    }
}