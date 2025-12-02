using UnityEngine;
using UnityEngine.Events; // Add this

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public string dialogueID;
    public bool triggerOnInteract = true;
    public bool triggerOnEnter = false;
    public bool oneTimeOnly = true;
    public float triggerDelay = 0f;
    
    [Header("Conditions")]
    public bool requiresItem = false;
    public string requiredItemID;
    public bool consumesItem = false;
    
    [Header("Visual Feedback")]
    public GameObject indicator;
    public bool showPrompt = true;
    public string customPromptText = "Talk";
    
    // Events
    public UnityEvent onDialogueTriggered;
    
    private bool hasTriggered = false;
    private PickupController playerPickup;
    
    void Start()
    {
        playerPickup = FindObjectOfType<PickupController>();
        
        if (indicator != null)
        {
            indicator.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle visual feedback
        if (showPrompt && indicator != null)
        {
            bool canInteract = CanTrigger();
            indicator.SetActive(canInteract);
        }
    }
    
    public void OnInteract()
    {
        if (triggerOnInteract && CanTrigger())
        {
            TriggerDialogue();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (triggerOnEnter && other.CompareTag("Player") && CanTrigger())
        {
            TriggerDialogue();
        }
    }
    
    bool CanTrigger()
    {
        if (oneTimeOnly && hasTriggered) return false;
        
        if (requiresItem)
        {
            if (playerPickup == null || playerPickup.HeldObject == null)
                return false;
                
            // Check if held item matches required item
            // You might want to create an Item component with an ID property
            // For now, we'll just check by name
            if (!string.IsNullOrEmpty(requiredItemID))
            {
                return playerPickup.HeldObject.name.Contains(requiredItemID);
            }
            return true;
        }
        
        return true;
    }
    
    void TriggerDialogue()
    {
        if (string.IsNullOrEmpty(dialogueID) || DialogueManager.Instance == null)
            return;
            
        if (triggerDelay > 0)
        {
            StartCoroutine(TriggerDelayed());
        }
        else
        {
            DialogueManager.Instance.StartDialogue(dialogueID);
            hasTriggered = true;
            onDialogueTriggered?.Invoke();
            
            if (requiresItem && consumesItem && playerPickup != null)
            {
                playerPickup.ForceDrop();
            }
        }
    }
    
    System.Collections.IEnumerator TriggerDelayed()
    {
        yield return new WaitForSeconds(triggerDelay);
        DialogueManager.Instance.StartDialogue(dialogueID);
        hasTriggered = true;
        onDialogueTriggered?.Invoke();
    }
    
    // For showing custom prompt in your PickupController
    public string GetPromptText()
    {
        if (!string.IsNullOrEmpty(customPromptText))
            return customPromptText;
            
        return requiresItem ? "Use" : "Talk";
    }
}