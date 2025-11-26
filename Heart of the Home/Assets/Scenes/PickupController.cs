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
    public float rotation_speed = 100f;
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

    private Collider[] player_colliders;
    private LayerMask collision_mask;

    private GameObject held_object;
    private Rigidbody held_object_rb;
    private bool is_inspecting = false;

    private float original_fov;
    public float zoomed_fov = 40f;

    private float bob_timer = 0f;
    private Vector3 target_inspect_position;
    private float object_radius;

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
    }

    void Update()
    {
        HandleLook();
        HandleInput();
        HandleInspectRotation();
        MoveHeldObject();
        UpdateVignette();
    }

    void HandleLook()
    {
        if (held_object != null)
        {
            // Update UI based on what we're looking at
            Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
            RaycastHit hit;
            bool show_interaction_prompt = false;

            if (Physics.Raycast(ray, out hit, pickup_range, collision_mask))
            {
                // Check if we're looking at a pedestal that can accept this item
                if (hit.collider.CompareTag("Pedestal"))
                {
                    var pedestal = hit.collider.GetComponent<pedestal_controller>();
                    if (pedestal != null)
                    {
                        show_interaction_prompt = true;
                        if (interaction_text != null)
                        {
                            interaction_text.text = "Press [E] to Place";
                            interaction_text.gameObject.SetActive(true);
                        }
                    }
                }
                // Check if we're looking at an interactable that can use this item
                else if (hit.collider.CompareTag("Interact"))
                {
                    show_interaction_prompt = true;
                    if (interaction_text != null)
                    {
                        interaction_text.text = "Press [E] to Use";
                        interaction_text.gameObject.SetActive(true);
                    }
                }
            }

            // If we're not showing a specific interaction prompt, show the default held object UI
            if (!show_interaction_prompt)
            {
                UpdateUIText("Press [R] to Inspect | [Q] to Drop | [Right Click] to Throw");
            }

            if (reticle != null)
                reticle.color = show_interaction_prompt ? highlight_color : highlight_color;
            
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

                        var door = hit.collider.GetComponent<door_lock_controller>();
                        if (door != null)
                        {
                            door.OnInteract();
                            return;
                        }

                        var ped = hit.collider.GetComponent<pedestal_controller>();
                        if (ped != null)
                        {
                            ped.OnInteract();
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

    void TryPickup()
    {
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

    void EnterInspectMode()
    {
        if (held_object == null) return;

        is_inspecting = true;
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
    }

    void ExitInspectMode()
    {
        if (held_object == null) return;

        is_inspecting = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        if (player_movement_script != null)
            player_movement_script.enabled = true;

        if (player_camera_script != null)
            player_camera_script.enabled = true;

        // Use a quick coroutine to smoothly return to hold position without breaking pedestal placement
        StartCoroutine(ReturnToHoldPosition());
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

    void DropObject()
    {
        if (held_object == null)
        {
            held_object_rb = null;
            is_inspecting = false;
            return;
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        if (player_movement_script != null)
            player_movement_script.enabled = true;

        if (player_camera_script != null)
            player_camera_script.enabled = true;

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

    void ThrowObject()
    {
        if (held_object == null || held_object_rb == null) return;

        held_object_rb.isKinematic = false;
        held_object_rb.useGravity = true;
        held_object_rb.AddForce(main_cam.transform.forward * throw_force, ForceMode.VelocityChange);

        held_object = null;
        held_object_rb = null;
        is_inspecting = false;
        object_radius = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        if (player_movement_script != null)
            player_movement_script.enabled = true;

        if (player_camera_script != null)
            player_camera_script.enabled = true;

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
    }

    void UpdateVignette()
    {
        if (vignette_image == null) return;

        float target_alpha = is_inspecting ? vignette_max_alpha : 0f;
        Color col = vignette_image.color;
        col.a = Mathf.Lerp(col.a, target_alpha, Time.deltaTime * vignette_speed);
        vignette_image.color = col;
    }

    void UpdateUIText(string message)
    {
        if (interaction_text != null)
        {
            interaction_text.text = message;
            interaction_text.gameObject.SetActive(true);
        }
    }
}