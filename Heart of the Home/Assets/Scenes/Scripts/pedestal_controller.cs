using UnityEngine;

public class pedestal_controller : MonoBehaviour
{
    public string expected_item_id;
    public Transform place_socket;
    public Light pedestal_light;
    public Color correct_color = Color.green;
    public Color incorrect_color = Color.red;

    private GameObject placed_item;
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
        GameObject held = pickup.HeldObject;
        if (held == null) return;

        item_component comp = held.GetComponent<item_component>();
        if (comp == null) return;

        placed_item = held;

        // Disable physics so item stays locked in place
        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;     // Freeze physics
            rb.useGravity = false;     // No falling
        }

        // IMPORTANT:
        // DO NOT disable the collider!
        // Instead, move item to a safe layer that doesn't collide with player/hands.
        held.layer = LayerMask.NameToLayer("PlacedItem"); // Must exist in Unity project

        // Ensure collider is ON so object doesn't fall through the floor
        Collider col = held.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;        // Must stay enabled so floor can support it
            col.isTrigger = false;     // Ensure it's solid
        }

        // Snap object into exact placement position
        held.transform.SetParent(place_socket);
        held.transform.localPosition = Vector3.zero;      // Perfect alignment
        held.transform.localRotation = Quaternion.identity;

        // Drop from player's inventory
        pickup.ForceDrop();

        // Check pedestal correctness
        is_correct = (comp.item_id == expected_item_id);
        
        // Update light state CORRECTLY
        UpdateLightState(is_correct);

        // Notify puzzle manager
        puzzle_manager.instance.Notify_pedestal_changed(this);
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

    // Optional: Method to force light state
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

    // Optional: Reset method
    public void ResetPedestal()
    {
        if (placed_item != null)
        {
            // Return item to interactable layer
            placed_item.layer = LayerMask.NameToLayer("Default");
            
            // Re-enable physics if needed
            Rigidbody rb = placed_item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            // Unparent
            placed_item.transform.SetParent(null);
            
            placed_item = null;
        }
        
        is_correct = false;
        UpdateLightState(false);
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
}