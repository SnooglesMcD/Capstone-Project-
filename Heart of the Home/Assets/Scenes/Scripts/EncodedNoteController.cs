// Scripts/EncodedNoteController.cs
using UnityEngine;

public class EncodedNoteController : MonoBehaviour
{
    [Header("Note Settings")]
    public AudioClip revealSound;
    public Material decodedMaterial;
    public GameObject decodedEffect;
    
    [Header("Text References")]
    public TextMesh frontText;
    public TextMesh backText;
    
    private bool isRevealed = false;
    private bool isDecoded = false;
    private Renderer noteRenderer;
    
    void Start()
    {
        gameObject.SetActive(false);
        noteRenderer = GetComponent<Renderer>();
        
        // Set up encoded text
        if (frontText != null)
        {
            frontText.text = "L mfdu uif voebufe xjmm jo uif mjcsbsz...";
        }
        
        if (backText != null)
        {
            backText.text = "Made the key a reminder of why we are so careful\nKey: Uncle's Name";
        }
    }
    
    public void Reveal()
    {
        if (isRevealed) return;
        
        gameObject.SetActive(true);
        isRevealed = true;
        
        if (revealSound != null)
        {
            AudioSource.PlayClipAtPoint(revealSound, transform.position);
        }
        
        Debug.Log("Encoded note revealed");
        
        // Add to pickup system
        gameObject.tag = "Note";
        
        // Add NotePickup component
        NotePickup notePickup = gameObject.GetComponent<NotePickup>();
        if (notePickup == null)
        {
            notePickup = gameObject.AddComponent<NotePickup>();
            notePickup.noteType = "encoded";
        }
    }
    
    public void OnDecoded()
    {
        if (isDecoded) return;
        
        isDecoded = true;
        
        // Change appearance
        if (decodedMaterial != null && noteRenderer != null)
        {
            noteRenderer.material = decodedMaterial;
        }
        
        // Show decoded text
        if (frontText != null)
        {
            frontText.text = "I left the updated will in the library with the changes for SILAS and his future protege.";
            frontText.color = Color.green;
        }
        
        // Play effect
        if (decodedEffect != null)
        {
            decodedEffect.SetActive(true);
        }
        
        Debug.Log("ENCODED NOTE DECODED: The will is in the library, changed for Silas");
    }
    
    public bool IsDecoded()
    {
        return isDecoded;
    }
}