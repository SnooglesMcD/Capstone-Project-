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

        // Snap and lock item
        placed_item = held;
        var rb = held.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        held.transform.position = place_socket.position;
        held.transform.rotation = place_socket.rotation;
        held.transform.SetParent(place_socket);

        // Drop it from player's hold
        pickup.ForceDrop();

        // Set correctness
        is_correct = (comp.item_id == expected_item_id);
        pedestal_light.enabled = is_correct;

        puzzle_manager.instance.Notify_pedestal_changed(this);
    }
}
