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
        Debug.Log($"Toybox_controller Start() called on {gameObject.name}");
        
        // Ensure collider is enabled
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
            Debug.Log($"Collider enabled: {col.enabled}");
        }
        else
        {
            Debug.LogError("No collider found on toybox!");
        }
        
        // Set up audio
        if (interactionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            Debug.Log("AudioSource added");
        }
    }
    
    public void OnInteract()
    {
        Debug.Log($"OnInteract() called on {gameObject.name}");
        
        // Check if DialogueManager exists
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance is NULL!");
            
            // Try to find it another way
            DialogueManager dm = FindObjectOfType<DialogueManager>();
            if (dm != null)
            {
                Debug.Log("Found DialogueManager via FindObjectOfType");
                // If your DialogueManager doesn't use Instance pattern, you might need to adjust this
            }
            else
            {
                Debug.LogError("No DialogueManager found in scene at all!");
            }
            return;
        }
        
        Debug.Log($"DialogueManager.Instance found: {DialogueManager.Instance.gameObject.name}");
        
        // Play dialogue
        if (!string.IsNullOrEmpty(dialogueId))
        {
            Debug.Log($"Attempting to start dialogue with ID: '{dialogueId}'");
            
            // Try different method names if StartDialogue doesn't work
            DialogueManager.Instance.StartDialogue(dialogueId);
            
    
        }
        else
        {
            Debug.LogWarning("dialogueId is empty or null!");
        }
        
        // Play sound
        if (audioSource != null && interactionSound != null)
        {
            audioSource.PlayOneShot(interactionSound);
            Debug.Log("Playing interaction sound");
        }
        
        Debug.Log($"Toy box interaction completed for: {gameObject.name}");
    }
    
    void OnMouseDown()
    {
        Debug.Log($"Mouse clicked on {gameObject.name}");
        OnInteract();
    }
}