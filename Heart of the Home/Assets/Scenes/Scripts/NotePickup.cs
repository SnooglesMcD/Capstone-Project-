// Scripts/NotePickup.cs
using UnityEngine;

public class NotePickup : MonoBehaviour
{
    [Header("Note Settings")]
    public string noteType = "generic"; // "monitor", "desk", "oscar", "affair", "torn_photo", "family_crest", "encoded"
    public AudioClip pickupSound;
    
    [Header("Content")]
    [TextArea(3, 10)]
    public string noteContent = "";
    
    private bool isCollected = false;
    
    public void OnInteract()
    {
        if (!isCollected)
        {
            CollectNote();
        }
    }
    
    void CollectNote()
    {
        isCollected = true;
        
        // Play sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.5f);
        }
        
        // Hide the note
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        
        // Disable collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Log content based on type
        switch (noteType.ToLower())
        {
            case "monitor":
                Debug.Log("Monitor note: Romantic message from Oscar");
                break;
            case "desk":
                Debug.Log("Desk note: 'Change alarm code to Victor's Birthday' - Annabell");
                break;
            case "affair":
                Debug.Log("Affair papers: Documents about settling affairs with Uncle Silas");
                break;
            case "oscar":
                Debug.Log("Oscar's note: 'All that matters is that we have each other... Don't seek revenge against Silas.'");
                break;
            case "torn_photo":
                Debug.Log("Torn family photo: Uncle Silas has been torn out");
                break;
            case "family_crest":
                Debug.Log("Family crest: The Silas family crest");
                break;
            case "encoded":
                Debug.Log("Encoded note: 'Made the key a reminder of why we are so careful. Key: Uncle's Name'");
                break;
            default:
                if (!string.IsNullOrEmpty(noteContent))
                {
                    Debug.Log($"Note: {noteContent}");
                }
                break;
        }
        
        // Notify puzzle manager for specific clues
        OfficePuzzleManager puzzleManager = FindObjectOfType<OfficePuzzleManager>();
        if (puzzleManager != null)
        {
            if (noteType.ToLower() == "affair" || noteType.ToLower() == "oscar" || 
                noteType.ToLower() == "torn_photo" || noteType.ToLower() == "family_crest")
            {
                puzzleManager.OnSilasClueDiscovered(noteType);
            }
        }
    }
}