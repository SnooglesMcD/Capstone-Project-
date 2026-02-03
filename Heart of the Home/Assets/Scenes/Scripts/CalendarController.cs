// Scripts/Puzzle/CalendarController.cs
using UnityEngine;

public class CalendarController : MonoBehaviour
{
    [Header("Settings")]
    public int markedDate = 1407;
    public GameObject heartMarker;
    public AudioClip interactSound;
    
    private bool hasBeenChecked = false;
    private OfficePuzzleManager puzzleManager;
    
    void Start()
    {
        puzzleManager = FindObjectOfType<OfficePuzzleManager>();
        
        if (heartMarker != null)
        {
            heartMarker.SetActive(false);
        }
    }
    
    public void OnInteract()
    {
        if (!hasBeenChecked)
        {
            hasBeenChecked = true;
            
            if (heartMarker != null)
            {
                heartMarker.SetActive(true);
            }
            
            if (interactSound != null)
            {
                AudioSource.PlayClipAtPoint(interactSound, transform.position);
            }
            
            if (puzzleManager != null)
            {
                puzzleManager.OnCalendarDateSelected(markedDate);
            }
            
            Debug.Log($"Calendar shows O+A in heart on date: {markedDate}");
        }
        else
        {
            Debug.Log($"Already checked calendar - date: {markedDate}");
        }
    }
}