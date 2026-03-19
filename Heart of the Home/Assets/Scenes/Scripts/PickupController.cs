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
    public Color safe_color = Color.yellow;
    public Color calendar_color = Color.magenta;

    [Header("Vignette")]
    public Image vignette_image;
    public float vignette_max_alpha = 0.25f;
    public float vignette_speed = 3f;

    [Header("Hold Bob Settings")]
    public float bob_amplitude = 0.05f;
    public float bob_speed = 2f;

    [Header("Player Movement")]
    public FirstPersonController player_movement_script;
    public MonoBehaviour player_camera_script;

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

    // Track if game was paused while inspecting
    private bool wasInspectingBeforePause = false;

    // Track if dialogue is active
    private bool isDialogueActive = false;

    private SafeController currentSafe;

    // Flag to prevent MoveHeldObject from interfering during drop/throw
    private bool isDroppingOrThrowing = false;


    // PUBLIC PROPERTIES FOR OTHER SCRIPTS
    public GameObject HeldObject => held_object;
    public bool IsInspecting => is_inspecting;
    public bool IsEnteringSafeCode => false;
    public pedestal_controller last_nearby_pedestal;


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

        player_colliders = GetComponentsInChildren<Collider>();
        collision_mask = ~(1 << LayerMask.NameToLayer("Player"));
        
        if (min_inspect_distance < main_cam.nearClipPlane + 0.1f)
        {
            min_inspect_distance = main_cam.nearClipPlane + 0.2f;
        }


        // Subscribe to DialogueManager events if exists
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart += OnDialogueStarted;
            DialogueManager.Instance.OnDialogueEnd += OnDialogueEnded;
        }

        Debug.Log("PickupController initialized for Office Puzzle");
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
            if (interaction_text != null && interaction_text.gameObject.activeSelf)
                interaction_text.gameObject.SetActive(false);
            
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
        Ray lookRay = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit lookHit;
        bool looking_at_interactable = false;

        currentSafe = null;

        if (Physics.Raycast(lookRay, out lookHit, pickup_range, collision_mask))
        {
            GameObject hitObject = lookHit.collider.gameObject;

            // Check for different interactable types
            if (hitObject.CompareTag("Safe"))
            {
                looking_at_interactable = true;
                UpdateUIText("Press [E] to Enter Code", safe_color);
                currentSafe = hitObject.GetComponent<SafeController>(); 
            }
            else if (hitObject.CompareTag("Note"))
            {
                looking_at_interactable = true;
                UpdateUIText("Press [E] to Read", highlight_color);
            }
            else if (held_object == null && (hitObject.CompareTag("Pickup") || hitObject.CompareTag("Book")))
            {
                looking_at_interactable = true;
                UpdateUIText("Press [E] to Pick Up", highlight_color);
            }
            else if (held_object != null && hitObject.CompareTag("Pedestal"))
            {
                looking_at_interactable = true;
                UpdateUIText("Press [E] to Place on Pedestal", highlight_color);
            }
            else if (hitObject.CompareTag("Door") || hitObject.CompareTag("Interact"))
            {
                looking_at_interactable = true;
                UpdateUIText("Press [E] to Interact", highlight_color);
            }
        }


        if (!looking_at_interactable)
        {
            if (interaction_text != null && interaction_text.gameObject.activeSelf)
            {
                interaction_text.gameObject.SetActive(false);
            }
        }

        if (reticle != null)
        {
            if (looking_at_interactable)
            {
                 reticle.color = highlight_color;
            }
            else
            {
                reticle.color = normal_color;
            }
        }
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

        // Don't process other input if dialogue is active
        if (isDialogueActive)
        {
            return;
        }

        // E key for interactions
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
                    // SAFE INTERACTION 
                    if (hit.collider.CompareTag("Safe"))
                    {
                        SafeController safe = hit.collider.GetComponent<SafeController>();
                        if (safe != null)
                        {
                            safe.ShowKeypad();
                        }
                        return;
                    }
                    else if (hit.collider.CompareTag("Pickup") || hit.collider.CompareTag("Book"))
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

        // Book reading with X (if holding a book)
        if (Input.GetKeyDown(KeyCode.X) && held_object != null && held_object.CompareTag("Book"))
        {
            BookController bookController = held_object.GetComponent<BookController>();
            if (bookController != null)
            {
                
                bool isReading = (bool)bookController.GetType().GetField("isBeingRead", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bookController);
                
                if (!isReading)
                {
                    bookController.StartReading();
                }
            }
        }
    }


    void TryUseHeldObject(GameObject target)
    {
        
        if (held_object.CompareTag("Key"))
        {
            // Generic door interaction instead of specific DoorLockController
            Debug.Log($"Trying to use {held_object.name} on {target.name}");
            DropObject(); // Drop key after use
            return;
        }
        
        // Add other object-use interactions here
    }

    void TryPickup()
    {
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
                    held_object_rb.linearVelocity = Vector3.zero;
                    held_object_rb.angularVelocity = Vector3.zero;
                    
                    // Store original drag values to restore later
                    held_object_rb.linearDamping = 0f;
                    held_object_rb.angularDamping = 0.05f;
                }

                held_object.transform.rotation = Quaternion.LookRotation(main_cam.transform.forward);
                held_object.transform.parent = hold_point;

                Collider obj_collider = held_object.GetComponent<Collider>();
                object_radius = obj_collider != null ? obj_collider.bounds.extents.magnitude : 0.25f;

                // Notify book controller if it's a book
                BookController bookController = held_object.GetComponent<BookController>();
                if (bookController != null)
                {
                    // Use reflection to call private method
                    System.Reflection.MethodInfo method = bookController.GetType().GetMethod("OnBookPickedUp",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(bookController, null);
                    
                    UpdateUIText("Press [X] to Read | [R] to Inspect | [Q] to Drop", highlight_color);
                }
                else
                {
                    UpdateUIText("Press [R] to Inspect | [Q] to Drop | [Right Click] to Throw", highlight_color);
                }

                Collider[] object_colliders = held_object.GetComponentsInChildren<Collider>();

                foreach (var pc in player_colliders)
                {
                    foreach (var oc in object_colliders)
                        Physics.IgnoreCollision(pc, oc, true);
                }
                
                Debug.Log($"Picked up: {held_object.name}");
            }
        }
    }

    void DropObject()
    {
        if (isDialogueActive || held_object == null) return;
        
        // Set flag to prevent MoveHeldObject from interfering
        isDroppingOrThrowing = true;
        
        // Notify book controller before dropping
        BookController bookController = held_object.GetComponent<BookController>();
        if (bookController != null)
        {
            System.Reflection.MethodInfo method = bookController.GetType().GetMethod("OnBookDropped",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(bookController, null);
        }

        // Store reference
        GameObject dropped_object = held_object;
        Rigidbody dropped_rb = held_object_rb;

        // CRITICAL: Detach from parent
        dropped_object.transform.parent = null;

        if (dropped_rb != null)
        {
            // Reset ALL rigidbody properties to default values
            dropped_rb.isKinematic = false;
            dropped_rb.useGravity = true;
            dropped_rb.linearVelocity = Vector3.zero;
            dropped_rb.angularVelocity = Vector3.zero;
            dropped_rb.linearDamping = 0.5f;      // Reset to default
            dropped_rb.angularDamping = 0.5f; // Reset to default
            dropped_rb.mass = 1f;         // Reset to default
            dropped_rb.interpolation = RigidbodyInterpolation.None;
            dropped_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            dropped_rb.constraints = RigidbodyConstraints.None;
            dropped_rb.WakeUp();
            
            // Add a significant downward force to ensure it falls
            dropped_rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
        }

        // Re-enable collisions
        Collider[] object_colliders = dropped_object.GetComponentsInChildren<Collider>();
        if (object_colliders != null)
        {
            foreach (var pc in player_colliders)
            {
                foreach (var oc in object_colliders)
                    Physics.IgnoreCollision(pc, oc, false);
            }
        }

        // Clear references
        held_object = null;
        held_object_rb = null;
        is_inspecting = false;
        object_radius = 0f;
        wasInspectingBeforePause = false;

        // Re-enable controls if we're not in inspect mode
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
            interaction_text.gameObject.SetActive(false);
        }
        
        // Reset flag after a short delay
        StartCoroutine(ResetDropThrowFlag());
        
        Debug.Log($"Dropped object: {dropped_object.name}");
    }

    void ThrowObject()
    {
        if (isDialogueActive || held_object == null || held_object_rb == null) return;

        // Set flag to prevent MoveHeldObject from interfering
        isDroppingOrThrowing = true;

        // Store reference
        GameObject thrown_object = held_object;
        Rigidbody thrown_rb = held_object_rb;

        // CRITICAL: Detach from parent
        thrown_object.transform.parent = null;

        // Reset ALL rigidbody properties
        thrown_rb.isKinematic = false;
        thrown_rb.useGravity = true;
        thrown_rb.linearVelocity = Vector3.zero;
        thrown_rb.angularVelocity = Vector3.zero;
        thrown_rb.linearDamping = 0.5f;
        thrown_rb.angularDamping = 0.5f;
        thrown_rb.mass = 1f;
        thrown_rb.interpolation = RigidbodyInterpolation.None;
        thrown_rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        thrown_rb.constraints = RigidbodyConstraints.None;
        thrown_rb.WakeUp();
        
        // Calculate throw direction (forward + slight up)
        Vector3 throwDirection = main_cam.transform.forward + (Vector3.up * 0.3f);
        
        // Apply throw force
        thrown_rb.AddForce(throwDirection.normalized * throw_force, ForceMode.Impulse);
        
        // Add some spin
        thrown_rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);

        // Re-enable collisions
        Collider[] object_colliders = thrown_object.GetComponentsInChildren<Collider>();
        if (object_colliders != null)
        {
            foreach (var pc in player_colliders)
            {
                foreach (var oc in object_colliders)
                    Physics.IgnoreCollision(pc, oc, false);
            }
        }

        // Clear references
        held_object = null;
        held_object_rb = null;
        is_inspecting = false;
        wasInspectingBeforePause = false;
        object_radius = 0f;

        // Re-enable controls if we're not in inspect mode
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
            interaction_text.gameObject.SetActive(false);
        }
        
        // Reset flag after a short delay
        StartCoroutine(ResetDropThrowFlag());
        
        Debug.Log($"Threw object: {thrown_object.name}");
    }

    System.Collections.IEnumerator ResetDropThrowFlag()
    {
        yield return new WaitForSeconds(0.2f);
        isDroppingOrThrowing = false;
    }

    void MoveHeldObject()
    {
        // Don't move the object if we're dropping or throwing or if it's null
        if (held_object == null || isDroppingOrThrowing) return;

        if (!is_inspecting)
        {
            bob_timer += Time.deltaTime * bob_speed;
            float bob_offset = Mathf.Sin(bob_timer) * bob_amplitude;
            Vector3 target_position = hold_point.position + new Vector3(0, bob_offset, 0);

            held_object.transform.position = target_position;
        }
        else
        {
            Vector3 desired_position = CalculateDesiredInspectPosition();
            Vector3 collision_adjusted_position = GetCollisionAdjustedPosition(desired_position);
            
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

    void EnterInspectMode()
    {
        if (held_object == null) return;

        is_inspecting = true;
        wasInspectingBeforePause = true;
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

        Vector3 desired_position = CalculateDesiredInspectPosition();
        Vector3 collision_adjusted_position = GetCollisionAdjustedPosition(desired_position);
        held_object.transform.position = collision_adjusted_position;
        
        UpdateUIText("Inspecting | [R] to Exit", highlight_color);
        
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
            if (dialogueID != "N/A")
            {
                isInspectionDialogueActive = true;
                DialogueManager.Instance.StartDialogue(dialogueID);
            }
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
        
        return "N/A";
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

        // Only re-enable controls if we're exiting inspect mode normally
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
            isInspectionDialogueActive = false;
        }
    }

    IEnumerator ReturnToHoldPosition()
    {
        if (held_object == null) yield break;
        
        Vector3 startPosition = held_object.transform.position;
        Vector3 targetPosition = hold_point.position + Vector3.up * post_inspect_lift;
        float elapsed = 0f;

        while (elapsed < 0.1f) // Very short duration for quick return
        {
            if (held_object == null) yield break;
            
            held_object.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (held_object != null)
        {
            held_object.transform.position = targetPosition;
        }
    }


    void UpdateVignette()
    {
        if (vignette_image == null) return;

        float target_alpha = is_inspecting ? vignette_max_alpha : 0f;
        Color col = vignette_image.color;
        col.a = Mathf.Lerp(col.a, target_alpha, Time.deltaTime * vignette_speed);
        vignette_image.color = col;
    }

    void UpdateUIText(string message, Color? color = null)
    {
        if (isDialogueActive) return;
        
        if (interaction_text != null)
        {
            interaction_text.text = message;
            interaction_text.gameObject.SetActive(true);
            
            if (color.HasValue)
            {
                interaction_text.color = color.Value;
            }
        }
    }

    IEnumerator ClearUITextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (interaction_text != null)
        {
            interaction_text.gameObject.SetActive(false);
        }
    }

    // Dialogue event handlers
    void OnDialogueStarted(Dialogue dialogue)
    {
        isDialogueActive = true;
        
        if (interaction_text != null)
            interaction_text.gameObject.SetActive(false);
            
        if (reticle != null)
            reticle.color = normal_color * 0.5f;
    }

    void OnDialogueEnded(Dialogue dialogue)
    {
        isDialogueActive = false;
        
        if (reticle != null)
            reticle.color = normal_color;
        
        if (isInspectionDialogueActive)
        {
            isInspectionDialogueActive = false;
        }
    }

    // Pause/resume handlers
    public void OnGamePaused()
    {
        wasInspectingBeforePause = is_inspecting;
        
        // Check if keypad is active and close it
        DynamicKeypad keypad = FindObjectOfType<DynamicKeypad>();
        if (keypad != null && keypad.IsActive())
        {
            keypad.CloseKeypad();
        }
    }

    public void OnGameResumed()
    {
        if (wasInspectingBeforePause && held_object != null)
        {
            if (player_movement_script != null)
                player_movement_script.enabled = false;

            if (player_camera_script != null)
                player_camera_script.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (interaction_text != null)
            {
                interaction_text.text = "Inspecting - Press [R] to Exit";
                interaction_text.gameObject.SetActive(true);
            }
        }
        else
        {
            wasInspectingBeforePause = false;
        
            // Don't lock cursor if keypad might be active
            DynamicKeypad keypad = FindObjectOfType<DynamicKeypad>();
            if (keypad == null || !keypad.IsActive())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // Public methods for external control
    public void ForceDrop()
    {
        DropObject();
    }

    public bool IsHoldingObject()
    {
        return held_object != null;
    }

    // Added for PauseMenu compatibility
    public bool ShouldControlsBeEnabled()
    {
        DynamicKeypad keypad = FindObjectOfType<DynamicKeypad>();
        bool keypadActive = (keypad != null && keypad.IsActive());
    
        return !is_inspecting && !keypadActive;
    }

    // Debug methods
    public void DebugPrintState()
    {
        Debug.Log($"PickupController State:");
        Debug.Log($"- Holding: {(held_object != null ? held_object.name : "Nothing")}");
        Debug.Log($"- Inspecting: {is_inspecting}");
        Debug.Log($"- Dialogue Active: {isDialogueActive}");
    }
}