using UnityEngine;

public class pedestal_controller : MonoBehaviour
{
    public string expected_item_id;
    public Transform place_socket;
    public Light pedestal_light;
    public Color correct_color = Color.green;
    public Color incorrect_color = Color.red;

    private GameObject placed_item;
    private Vector3 original_item_scale; // Store the original scale
    public bool is_correct;

    void Start()
    {
        // Initialize light state
        if (pedestal_light != null)
        {
            // Make sure both GameObject and Light component are properly set
            pedestal_light.gameObject.SetActive(true);
            pedestal_light.enabled = false; // Start disabled
        }
    }

    public void OnInteract()
    {
        // Player must be holding an item
        var pickup = FindObjectOfType<PickupController>();
        if (pickup == null)
        {
            Debug.LogWarning("No PickupController found in scene!");
            return;
        }
        
        GameObject held = pickup.HeldObject;
        if (held == null) return;

        item_component comp = held.GetComponent<item_component>();
        if (comp == null) return;

        placed_item = held;

        // Save the original world scale BEFORE parenting
        original_item_scale = held.transform.lossyScale;

        // Disable physics so item stays locked in place
        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;     // Freeze physics
            rb.useGravity = false;     // No falling
        }

        

        
        Collider col = held.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;        // Must stay enabled so floor can support it
            col.isTrigger = false;     // Ensure it's solid
        }

        // Parent the item to the socket while preserving world position/rotation
        held.transform.SetParent(place_socket, false);
        
        // Set position and rotation to match socket
        held.transform.position = place_socket.position;
        held.transform.rotation = place_socket.rotation;
        
        
        // Calculate and set the local scale to preserve the original world scale
        // This prevents distortion from parent scale
        if (place_socket != null)
        {
            Vector3 parentLossyScale = place_socket.lossyScale;
            
            // Avoid division by zero
            if (Mathf.Abs(parentLossyScale.x) > 0.001f && 
                Mathf.Abs(parentLossyScale.y) > 0.001f && 
                Mathf.Abs(parentLossyScale.z) > 0.001f)
            {
                held.transform.localScale = new Vector3(
                    original_item_scale.x / parentLossyScale.x,
                    original_item_scale.y / parentLossyScale.y,
                    original_item_scale.z / parentLossyScale.z
                );
            }
            else
            {
                // Fallback: set uniform scale
                held.transform.localScale = Vector3.one;
                Debug.LogWarning("Pedestal socket has near-zero scale, using uniform scaling instead.");
            }
        }

        // Drop from player's inventory
        pickup.ForceDrop();
        rb.isKinematic = true; 

        // Check pedestal correctness
        is_correct = (comp.item_id == expected_item_id);
        
        // Update light state CORRECTLY
        UpdateLightState(is_correct);

        // Notify puzzle manager
        puzzle_manager.instance.Notify_pedestal_changed(this);
        
        Debug.Log($"Item placed. Correct: {is_correct}, Original scale: {original_item_scale}, Current scale: {held.transform.lossyScale}");
    }

    void UpdateLightState(bool correct)
    {
        if (pedestal_light != null)
        {
            // FIRST ensure GameObject is active
            pedestal_light.gameObject.SetActive(true);
            
            // THEN enable/disable the Light component
            pedestal_light.enabled = correct;
            
            // Set color based on correctness
            pedestal_light.color = correct ? correct_color : incorrect_color;
            
            Debug.Log($"Pedestal light: GameObject active={pedestal_light.gameObject.activeSelf}, " +
                     $"Light enabled={pedestal_light.enabled}, Correct={correct}");
        }
    }

    // Method to force light state
    public void SetLight(bool enabled, Color? color = null)
    {
        if (pedestal_light != null)
        {
            // Ensure GameObject is active
            if (!pedestal_light.gameObject.activeSelf)
                pedestal_light.gameObject.SetActive(true);
            
            // Set Light component state
            pedestal_light.enabled = enabled;
            
            // Set color if provided
            if (color.HasValue)
                pedestal_light.color = color.Value;
        }
    }

    

    // Debug method to check light state
    public void DebugLightState()
    {
        if (pedestal_light != null)
        {
            Debug.Log($"Light GameObject active: {pedestal_light.gameObject.activeSelf}");
            Debug.Log($"Light component enabled: {pedestal_light.enabled}");
            Debug.Log($"Light component exists: {pedestal_light != null}");
            Debug.Log($"Light color: {pedestal_light.color}");
            Debug.Log($"Light intensity: {pedestal_light.intensity}");
        }
        else
        {
            Debug.LogWarning("pedestal_light is null!");
        }
    }
    
    // Helper method to debug scale information
    public void DebugScaleInfo()
    {
        if (placed_item != null)
        {
            Debug.Log($"Item: {placed_item.name}");
            Debug.Log($"Local Scale: {placed_item.transform.localScale}");
            Debug.Log($"World Scale: {placed_item.transform.lossyScale}");
            Debug.Log($"Parent Socket Scale: {place_socket.lossyScale}");
        }
        
        if (place_socket != null)
        {
            Debug.Log($"Socket full hierarchy:");
            Transform current = place_socket;
            while (current != null)
            {
                Debug.Log($"  {current.name} - Local Scale: {current.localScale}");
                current = current.parent;
            }
        }
    }
}