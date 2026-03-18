using UnityEngine;

public class floor_board_controller : MonoBehaviour
{
    public GameObject key_prefab;
    public string locked_dialogue_id = "floorboard_locked";
    public float locked_message_duration = 2f;
    private bool opened = false;

    void Start()
    {
        // Start with key hidden
        if (key_prefab != null)
        {
            key_prefab.SetActive(false);
        }
        
        // Make sure collider is ALWAYS enabled for raycast detection
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }

    public void OnInteract()
    {
        if (opened) return;

        // Check if puzzle is solved
        bool puzzleSolved = IsPuzzleSolved();
        
        if (!puzzleSolved)
        {
            // Puzzle NOT solved - show locked dialogue
            ShowLockedDialogue();
            return;
        }
        
        // Puzzle IS solved - open floorboard
        OpenFloorboard();
    }

    bool IsPuzzleSolved()
    {
        if (puzzle_manager.instance == null)
        {
            Debug.LogWarning("No puzzle_manager found!");
            return false;
        }
        
        if (puzzle_manager.instance.prism_light == null)
        {
            Debug.LogWarning("No prism light in puzzle_manager!");
            return false;
        }
        
        return puzzle_manager.instance.prism_light.gameObject.activeInHierarchy && 
               puzzle_manager.instance.prism_light.enabled;
    }

    void ShowLockedDialogue()
    {
        Debug.Log("Showing locked dialogue for floorboard");
        
        if (DialogueManager.Instance != null)
        {
            // Try to show specific locked dialogue
            if (!string.IsNullOrEmpty(locked_dialogue_id))
            {
                DialogueManager.Instance.StartDialogue(locked_dialogue_id);
            }
        }
    }

    void OpenFloorboard()
    {
        opened = true;
        
        // Spawn key
        if (key_prefab != null)
        {
            key_prefab.SetActive(true);
            
            // Position above floorboard
            key_prefab.transform.position = transform.position + Vector3.up * 0.3f;
        }
        
        Debug.Log("Floorboard opened and key spawned");
    }
    
    void PlayLockedFeedback()
    {
        // Add particle effect, sound, etc.
    }
}