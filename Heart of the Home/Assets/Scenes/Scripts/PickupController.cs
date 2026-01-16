using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PickupController : MonoBehaviour
{
    [Header("References")]
    public Camera main_cam;
    public Transform hold_point;
    public Transform inspect_point;

    [Header("Settings")]
    public float pickup_range = 3f;
    public float move_force = 250f;
    public float inspect_zoom_speed = 4f;
    public float rotation_speed = 360f;
    public float post_inspect_lift = 0.3f;
    public float lift_duration = 0.2f;
    public float inspect_distance_offset = 0.25f;
    public float min_inspect_distance = 0.5f;

    [Header("Throw Settings")]
    public float throw_force = 10f;

    [Header("UI Elements")]
    public TextMeshProUGUI interaction_text;
    public Image reticle;
    public Color normal_color = Color.white;
    public Color highlight_color = Color.cyan;

    [Header("Vignette")]
    public Image vignette_image;
    public float vignette_max_alpha = 0.25f;
    public float vignette_speed = 3f;

    [Header("Hold Bob Settings")]
    public float bob_amplitude = 0.05f;
    public float bob_speed = 2f;

    [Header("Player Movement")]
    public MonoBehaviour player_movement_script;
    public MonoBehaviour player_camera_script;

    [Header("Public Variables")]
    public bool IsInspecting => is_inspecting;
    public GameObject HeldObject => held_object;

    [Header("Inspection Integration")]
    public bool showInspectionDialogue = true;
    public float inspectionDelay = 0.5f;    

    private Collider[] player_colliders;
    private LayerMask collision_mask;

    private GameObject held_object;
    private Rigidbody held_object_rb;
    private bool is_inspecting = false;
    private Coroutine inspectionCoroutine;
    private bool isInspectionDialogueActive = false; 

    private float original_fov;
    public float zoomed_fov = 40f;

    private float bob_timer = 0f;
    private Vector3 target_inspect_position;
    private float object_radius;

    private pedestal_controller last_nearby_pedestal;

    //Track if game was paused while inspecting
    private bool wasInspectingBeforePause = false;

    //Track if dialogue is active
    private bool isDialogueActive = false;

    void Start()
    {
        if (interaction_text != null)
            interaction_text.gameObject.SetActive(false);

        if (main_cam != null)
            original_fov = main_cam.fieldOfView;

        if (vignette_image != null)
        {
            var col = vignette_image.color;
            col.a = 0f;
            vignette_image.color = col;
        }

        if (interaction_text != null) interaction_text.gameObject.SetActive(false);

        player_colliders = GetComponentsInChildren<Collider>();
        collision_mask = ~(1 << LayerMask.NameToLayer("Player"));
        
        if (min_inspect_distance < main_cam.nearClipPlane + 0.1f)
        {
            min_inspect_distance = main_cam.nearClipPlane + 0.2f;
        }

        // Subscribe to DialogueManager events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart += OnDialogueStarted;
            DialogueManager.Instance.OnDialogueEnd += OnDialogueEnded;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from DialogueManager events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart -= OnDialogueStarted;
            DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnded;
        }
    }

    void Update()
    {
        HandleLook();
        HandleInput();
        HandleInspectRotation();
        MoveHeldObject();
        UpdateVignette();

        // Skip UI updates if dialogue is active
        if (isDialogueActive)
        {
            // Hide UI elements when dialogue is active
            if (interaction_text != null && interaction_text.gameObject.activeSelf)
                interaction_text.gameObject.SetActive(false);
            
            // Keep reticle visible but  dimmed
            if (reticle != null)
                reticle.color = normal_color * 0.5f;
                
            return;
        }
        
        
    }

    void HandleLook()
    {
        if (held_object != null)
        {
            // Find all pedestals in range
            pedestal_controller[] all_pedestals = FindObjectsOfType<pedestal_controller>();
            pedestal_controller nearest_pedestal = null;
            float nearest_distance = float.MaxValue;
            
            foreach (var pedestal in all_pedestals)
            {
                float distance = Vector3.Distance(transform.position, pedestal.transform.position);
                if (distance <= pickup_range)
                {
                    // Check if we're looking at or near this pedestal
                    Vector3 direction_to_pedestal = (pedestal.transform.position - main_cam.transform.position).normalized;
                    float angle = Vector3.Angle(main_cam.transform.forward, direction_to_pedestal);
                    
                    // More generous angle check (up to 30 degrees)
                    if (angle < 30f && distance < nearest_distance)
                    {
                        nearest_pedestal = pedestal;
                        nearest_distance = distance;
                    }
                }
            }
            
            // Show interaction prompt if we found a pedestal
            if (nearest_pedestal != null)
            {
                if (interaction_text != null)
                {
                    interaction_text.text = "Press [E] to Place";
                    interaction_text.gameObject.SetActive(true);
                }
                
                // Store the nearest pedestal for interaction
                last_nearby_pedestal = nearest_pedestal;
            }
            else
            {
                // Default held object UI
                UpdateUIText("Press [R] to Inspect | [Q] to Drop | [Right Click] to Throw");
            }
            
            if (reticle != null)
                reticle.color = (nearest_pedestal != null) ? highlight_color : highlight_color;
            
            return;
        }

        // Original code for when not holding an object
        Ray lookRay = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit lookHit;
        bool looking_at_pickup = false;

        if (Physics.Raycast(lookRay, out lookHit, pickup_range, collision_mask))
        {
            if (held_object == null && (lookHit.collider.CompareTag("Pickup") || lookHit.collider.CompareTag("Book")))
            {
                looking_at_pickup = true;
                if (interaction_text != null)
                {
                    interaction_text.text = "Press [E] to Pick Up";
                    interaction_text.gameObject.SetActive(true);
                }
            }
        }

        if (Physics.Raycast(lookRay, out lookHit, pickup_range, collision_mask))
        {
            // If not holding and looking at generic interactable (door/floorboard), show interact prompt
            if (held_object == null && (lookHit.collider.CompareTag("Interact")))
            {
                looking_at_pickup = true;
                if (interaction_text != null)
                {
                    interaction_text.text = "Press [E] to Interact";
                    interaction_text.gameObject.SetActive(true);
                }
            }

            // If holding and looking at door (to use key), show "Use" prompt
            else if (held_object != null && (lookHit.collider.CompareTag("Interact")))
            {
                // Show a "Use" prompt (e.g., use key on door)
                looking_at_pickup = true;
                if (interaction_text != null)
                {
                    interaction_text.text = "Press [E] to Use";
                    interaction_text.gameObject.SetActive(true);
                }
            }
        }

        if (!looking_at_pickup && interaction_text != null && held_object == null)
            interaction_text.gameObject.SetActive(false);

        if (reticle != null)
            reticle.color = looking_at_pickup ? highlight_color : normal_color;
    }

    void HandleInput()
    {
        // R key for inspect/uninspect
        if (Input.GetKeyDown(KeyCode.R) && held_object != null)
        {
            if (!is_inspecting)
            {
                EnterInspectMode();
            }
            else
            {
                ExitInspectMode();
            }
            return;
        }

        //  Don't process input if dialogue is active (except for closing dialogue)
        if (isDialogueActive)
        {
            return;
        }

        

        // E key for interactions (pick up, place, use)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (held_object != null)
            {
                // If holding an item, E is for placing/using
                Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, pickup_range, collision_mask))
                {
                    if (hit.collider.CompareTag("Pedestal"))
                    {
                        var ped = hit.collider.GetComponent<pedestal_controller>();
                        if (ped != null)
                        {
                            ped.OnInteract();
                            return;
                        }
                    }
                    else if (hit.collider.CompareTag("Interact"))
                    {
                        var door = hit.collider.GetComponent<door_lock_controller>();
                        if (door != null)
                        {
                            door.KeyCollected();
                            door.OnInteract();
                            return;
                        }

                        var fb = hit.collider.GetComponent<floor_board_controller>();
                        if (fb != null)
                        {
                            fb.OnInteract();
                            return;
                        }
                    }
                }
                
                // Use proximity-based pedestal (more forgiving)
                if (last_nearby_pedestal != null)
                {
                    float distance = Vector3.Distance(transform.position, last_nearby_pedestal.transform.position);
                    if (distance <= pickup_range)
                    {
                        last_nearby_pedestal.OnInteract();
                        last_nearby_pedestal = null;
                        return;
                    }
                }
            }
            else
            {
                // If not holding an item, E is for picking up or interacting
                Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, pickup_range, collision_mask))
                {
                    if (hit.collider.CompareTag("Pickup") || hit.collider.CompareTag("Book"))
                    {
                        TryPickup();
                        return;
                    }
                    else if (hit.collider.CompareTag("Interact"))
                    {
                        var fb = hit.collider.GetComponent<floor_board_controller>();
                        if (fb != null)
                        {
                            fb.OnInteract();
                            return;
                        }

                        var toybox = hit.collider.GetComponent<Toybox_controller>();
                        if (toybox != null)
                        {
                            toybox.OnInteract();
                            return;
                        }

                        var ped = hit.collider.GetComponent<pedestal_controller>();
                        if (ped != null)
                        {
                            ped.OnInteract();
                            return;
                        }

                        var door = hit.collider.GetComponent<door_lock_controller>();
                        if (door != null)
                        {
                            door.OnInteract();
                            return;
                        }
                    }
                }

                // Fallback pickup attempt
                TryPickup();
            }
        }

        // Drop with Q
        if (Input.GetKeyDown(KeyCode.Q) && held_object != null)
        {
            DropObject();
        }

        // Throw with right mouse button
        if (Input.GetMouseButtonDown(1) && held_object != null && !is_inspecting)
        {
            ThrowObject();
        }
    }

    // In the UpdateUIText method or similar location:
    void UpdateUIText(string message)
    {
        // Don't update UI if dialogue is active
        if (isDialogueActive) return;
        
        if (interaction_text != null)
        {
            // Check if holding a book and add reading prompt
            if (held_object != null && held_object.CompareTag("Book"))
            {
                message += " | [X] to Read";
            }
            
            interaction_text.text = message;
            interaction_text.gameObject.SetActive(true);
        }
    }

    // In the TryPickup method, add book controller notification:
    void TryPickup()
    {
        // Don't allow pickup if dialogue is active
        if (isDialogueActive) return;
        
        Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickup_range, collision_mask))
        {
            if (hit.collider.CompareTag("Pickup") || hit.collider.CompareTag("Book"))
            {
                held_object = hit.collider.gameObject;
                held_object_rb = held_object.GetComponent<Rigidbody>();

                if (held_object_rb != null)
                {
                    held_object_rb.useGravity = false;
                    held_object_rb.isKinematic = true;
                }

                held_object.transform.rotation = Quaternion.LookRotation(main_cam.transform.forward);

                Collider obj_collider = held_object.GetComponent<Collider>();
                object_radius = obj_collider != null ? obj_collider.bounds.extents.magnitude : 0.25f;

                // Notify book controller that it was picked up
                BookController bookController = held_object.GetComponent<BookController>();
                if (bookController != null)
                {
                    bookController.OnBookPickedUp();
                }

                UpdateUIText("Press [R] to Inspect | [Q] to Drop | [Right Click] to Throw");

                Collider[] object_colliders = held_object.GetComponentsInChildren<Collider>();

                foreach (var pc in player_colliders)
                {
                    foreach (var oc in object_colliders)
                        Physics.IgnoreCollision(pc, oc, true);
                }
            }
        }
    }

    // In the DropObject method, add book controller notification:
    void DropObject()
    {
        //  Don't allow drop if dialogue is active
        if (isDialogueActive) return;
        
        if (held_object == null)
        {
            held_object_rb = null;
            is_inspecting = false;
            wasInspectingBeforePause = false; // Reset pause tracking
            return;
        }

        // Notify book controller before dropping
        BookController bookController = held_object.GetComponent<BookController>();
        if (bookController != null)
        {
            bookController.OnBookDropped();
        }

        if (held_object_rb != null)
        {
            held_object_rb.isKinematic = false;
            held_object_rb.useGravity = true;
        }

        GameObject dropped_object = held_object; 

        held_object = null;
        held_object_rb = null;
        is_inspecting = false;
        object_radius = 0f;
        wasInspectingBeforePause = false; // Reset pause tracking

        // Only re-enable controls if we're not in inspect mode
        if (!is_inspecting)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (player_movement_script != null)
                player_movement_script.enabled = true;

            if (player_camera_script != null)
                player_camera_script.enabled = true;
        }

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        if (interaction_text != null)
        {
            interaction_text.text = "";
            interaction_text.gameObject.SetActive(false);
        }

        if (dropped_object != null)
        {
            Collider[] object_colliders = dropped_object.GetComponentsInChildren<Collider>();

            if (object_colliders != null)
            {
                foreach (var pc in player_colliders)
                {
                    foreach (var oc in object_colliders)
                        Physics.IgnoreCollision(pc, oc, false);
                }
            }
        }
    }

    void MoveHeldObject()
    {
        if (held_object == null) return;

        if (!is_inspecting)
        {
            // 1:1 movement with player - no lerp, direct position assignment
            bob_timer += Time.deltaTime * bob_speed;
            float bob_offset = Mathf.Sin(bob_timer) * bob_amplitude;
            Vector3 target_position = hold_point.position + new Vector3(0, bob_offset, 0);

            // Direct position assignment for perfect 1:1 movement
            held_object.transform.position = target_position;
        }
        else
        {
            // For inspection: direct position with collision detection
            Vector3 desired_position = CalculateDesiredInspectPosition();
            Vector3 collision_adjusted_position = GetCollisionAdjustedPosition(desired_position);
            
            // Direct position assignment for perfect 1:1 movement
            held_object.transform.position = collision_adjusted_position;

            main_cam.fieldOfView = Mathf.Lerp(
                main_cam.fieldOfView,
                zoomed_fov,
                Time.deltaTime * inspect_zoom_speed
            );
        }
    }

    Vector3 CalculateDesiredInspectPosition()
    {
        Vector3 camera_forward = main_cam.transform.forward;
        Vector3 camera_position = main_cam.transform.position;
        
        // Calculate position directly in front of camera
        Vector3 base_position = camera_position + camera_forward * inspect_distance_offset;
        
        return base_position;
    }

    Vector3 GetCollisionAdjustedPosition(Vector3 desiredPosition)
    {
        Vector3 camera_position = main_cam.transform.position;
        Vector3 direction_to_desired = (desiredPosition - camera_position).normalized;
        float distance_to_desired = Vector3.Distance(camera_position, desiredPosition);

        RaycastHit hit;
        float sphere_cast_distance = distance_to_desired + object_radius;
        
        if (Physics.SphereCast(camera_position, object_radius, direction_to_desired, out hit, sphere_cast_distance, collision_mask, QueryTriggerInteraction.Ignore))
        {
            float safe_distance = Mathf.Max(hit.distance - object_radius, min_inspect_distance + object_radius);
            Vector3 safe_position = camera_position + direction_to_desired * safe_distance;
            
            return safe_position;
        }

        float current_distance = Vector3.Distance(camera_position, desiredPosition);
        if (current_distance < min_inspect_distance + object_radius)
        {
            return camera_position + direction_to_desired * (min_inspect_distance + object_radius);
        }

        return desiredPosition;
    }

    void HandleInspectRotation()
    {
        if (held_object == null || !is_inspecting) return;

        float mouse_x = Input.GetAxis("Mouse X") * rotation_speed * Time.deltaTime;
        float mouse_y = Input.GetAxis("Mouse Y") * rotation_speed * Time.deltaTime;

        held_object.transform.Rotate(main_cam.transform.up, -mouse_x, Space.World);
        held_object.transform.Rotate(main_cam.transform.right, mouse_y, Space.World);
    }


    // Update EnterInspectMode():
    void EnterInspectMode()
    {
        
        if (held_object == null) return;

        is_inspecting = true;
        wasInspectingBeforePause = true; // Track that we were inspecting
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (player_movement_script != null)
            player_movement_script.enabled = false;

        if (player_camera_script != null)
            player_camera_script.enabled = false;

        if (object_radius <= 0f)
        {
            Collider obj_collider = held_object.GetComponent<Collider>();
            object_radius = obj_collider != null ? obj_collider.bounds.extents.magnitude : 0.25f;
        }

        // Immediate positioning
        Vector3 desired_position = CalculateDesiredInspectPosition();
        Vector3 collision_adjusted_position = GetCollisionAdjustedPosition(desired_position);
        held_object.transform.position = collision_adjusted_position;
        
        // Show inspection dialogue after delay
        if (showInspectionDialogue && DialogueManager.Instance != null)
        {
            if (inspectionCoroutine != null)
            {
                StopCoroutine(inspectionCoroutine);
                inspectionCoroutine = null;
            }
            
            // Reset dialogue flag
            isInspectionDialogueActive = false;
            
            inspectionCoroutine = StartCoroutine(ShowInspectionDialogue());
        }
    }

    IEnumerator ShowInspectionDialogue()
    {
        yield return new WaitForSeconds(inspectionDelay);
        
        // Get the item_component to retrieve the item_id
        item_component itemComp = held_object.GetComponent<item_component>();
        if (itemComp != null && !string.IsNullOrEmpty(itemComp.item_id))
        {
            // Try to show inspection dialogue using the item_id
            string dialogueID = itemComp.item_id;
            isInspectionDialogueActive = true;
            DialogueManager.Instance.StartDialogue(dialogueID);
        }
        else
        {
            // Fallback to name-based dialogue ID
            string dialogueID = GetInspectionDialogueID(held_object);
            isInspectionDialogueActive = true;
            DialogueManager.Instance.StartDialogue(dialogueID);
        }
    }

    string GetInspectionDialogueID(GameObject obj)
    {
        // First try to get item_component
        item_component itemComp = obj.GetComponent<item_component>();
        if (itemComp != null && !string.IsNullOrEmpty(itemComp.item_id))
        {
            return itemComp.item_id;
        }
        
        // Fallback to name-based detection
        string itemName = obj.name.ToLower();
        
        if (itemName.Contains("vulture")) return "vulture";
        if (itemName.Contains("bust")) return "bust";
        if (itemName.Contains("evil_eye")) return "evil_eye";
        if (itemName.Contains("book")) return "book";
        if (itemName.Contains("key")) return "key";
        
        else return "N/A";
        
    }

    
    public void ExitInspectMode()
    {
        
        if (held_object == null) return;

        is_inspecting = false;
        wasInspectingBeforePause = false; // Reset pause tracking
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        //  Only re-enable controls if we're exiting inspect mode normally
        // (not just because game was paused/unpaused)
        if (player_movement_script != null)
            player_movement_script.enabled = true;

        if (player_camera_script != null)
            player_camera_script.enabled = true;

        // Clear any active inspection dialogue
        ClearInspectionDialogue();

        // Stop any ongoing inspection coroutine
        if (inspectionCoroutine != null)
        {
            StopCoroutine(inspectionCoroutine);
            inspectionCoroutine = null;
        }
        
        // Reset the flag
        isInspectionDialogueActive = false;
        
        // Use a quick coroutine to smoothly return to hold position
        StartCoroutine(ReturnToHoldPosition());
    }

    void ClearInspectionDialogue()
    {
        if (isInspectionDialogueActive && DialogueManager.Instance != null)
        {
            // Check if dialogue is actually active
            if (DialogueManager.Instance.IsDialogueActive())
            {
                DialogueManager.Instance.ForceEndDialogue();
            }
        }
    }

    IEnumerator ReturnToHoldPosition()
    {
        Vector3 startPosition = held_object.transform.position;
        Vector3 targetPosition = hold_point.position + Vector3.up * post_inspect_lift;
        float elapsed = 0f;

        while (elapsed < 0.1f) // Very short duration for quick return
        {
            held_object.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        held_object.transform.position = targetPosition;
    }


    void ThrowObject()
    {
        //  Don't allow throwing if dialogue is active
        if (isDialogueActive) return;
        
        if (held_object == null || held_object_rb == null) return;

        held_object_rb.isKinematic = false;
        held_object_rb.useGravity = true;
        held_object_rb.AddForce(main_cam.transform.forward * throw_force, ForceMode.VelocityChange);

        held_object = null;
        held_object_rb = null;
        is_inspecting = false;
        wasInspectingBeforePause = false; // Reset pause tracking
        object_radius = 0f;

        // Only re-enable controls if we're not in inspect mode
        if (!is_inspecting)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (player_movement_script != null)
                player_movement_script.enabled = true;

            if (player_camera_script != null)
                player_camera_script.enabled = true;
        }

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        if (interaction_text != null)
        {
            interaction_text.text = "";
            interaction_text.gameObject.SetActive(false);
        }
    }

    public void ForceDrop()
    {
        if (held_object == null)
        {
            held_object_rb = null;
            wasInspectingBeforePause = false; // Reset pause tracking
            return;
        }

        if (held_object_rb != null)
        {
            held_object_rb.isKinematic = false;
            held_object_rb.useGravity = true;
        }

        held_object = null;
        held_object_rb = null;
        object_radius = 0f;
        wasInspectingBeforePause = false; // Reset pause tracking
    }

    void UpdateVignette()
    {
        if (vignette_image == null) return;

        float target_alpha = is_inspecting ? vignette_max_alpha : 0f;
        Color col = vignette_image.color;
        col.a = Mathf.Lerp(col.a, target_alpha, Time.deltaTime * vignette_speed);
        vignette_image.color = col;
    }

    // METHODS TO HANDLE PAUSE/UNPAUSE WHILE INSPECTING
    public void OnGamePaused()
    {
        // Store whether we were inspecting before pause
        wasInspectingBeforePause = is_inspecting;
        
        if (is_inspecting)
        {
            // While inspecting, we don't want to change cursor state on pause
            // because the pause menu will handle its own cursor
        }
    }

    public void OnGameResumed()
    {
        // Check if we were inspecting before pause
        if (wasInspectingBeforePause && held_object != null)
        {
            // We were inspecting when game was paused
            // Don't re-enable player controls - stay in inspect mode
            if (player_movement_script != null)
                player_movement_script.enabled = false;

            if (player_camera_script != null)
                player_camera_script.enabled = false;

            // Keep cursor unlocked and visible for inspect mode
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Update UI to show we're still in inspect mode
            if (interaction_text != null)
            {
                interaction_text.text = "Inspecting - Press [R] to Exit";
                interaction_text.gameObject.SetActive(true);
            }
        }
        else
        {
            // We weren't inspecting, or we don't have an object
            wasInspectingBeforePause = false;
            
            // Normal cursor state for non-inspect mode
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Helper method to check if controls should be enabled
    public bool ShouldControlsBeEnabled()
    {
        // Controls should be enabled if we're not inspecting
        return !is_inspecting;
    }

    // Dialogue event handlers
    void OnDialogueStarted(Dialogue dialogue)
    {
        isDialogueActive = true;
        
        // Hide interaction UI when dialogue starts
        if (interaction_text != null)
            interaction_text.gameObject.SetActive(false);
            
        // Dim the reticle
        if (reticle != null)
            reticle.color = normal_color * 0.5f;
            
        Debug.Log("Dialogue started - hiding interaction UI");
    }

    void OnDialogueEnded(Dialogue dialogue)
    {
        isDialogueActive = false;
        
        // Reset reticle to normal
        if (reticle != null)
            reticle.color = normal_color;
            
        Debug.Log("Dialogue ended - interaction UI can be shown again");
        
        // If we were in inspection dialogue, mark it as no longer active
        if (isInspectionDialogueActive)
        {
            isInspectionDialogueActive = false;
        }
    }

    // Check if dialogue is active
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}