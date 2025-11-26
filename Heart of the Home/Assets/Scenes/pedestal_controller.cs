using UnityEngine;

public class pedestal_controller : MonoBehaviour
{
    public string expected_item_id;
    public Transform place_socket;
    public Light pedestal_light;

    private GameObject placed_item;
    public bool is_correct;

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
        pedestal_light.enabled = is_correct;
        if (pedestal_light != null)
        {
            pedestal_light.gameObject.SetActive(true);
        }

        // Notify puzzle manager
        puzzle_manager.instance.Notify_pedestal_changed(this);
    }
}
