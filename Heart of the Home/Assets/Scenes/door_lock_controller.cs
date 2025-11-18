using UnityEngine;

public class door_lock_controller : MonoBehaviour
{
    public string required_key_id = "office_key";
    public Animator door_animator;
    

    public void OnInteract()
    {
        var pickup = FindObjectOfType<PickupController>();
        GameObject held = pickup.HeldObject;

        if (held == null) return;

        var comp = held.GetComponent<item_component>();
    }
}
