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
    public float inspect_distance_offset = 0.25f; // How far back from inspect_point to keep object

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

    private Collider player_colliders;  // reference to player collider



    // Runtime variables
    private GameObject held_object;
    private Rigidbody held_object_rb;
    private bool is_inspecting = false;

    private float original_fov;
    public float zoomed_fov = 40f;

    private float bob_timer = 0f;

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

    // cache player collider (assuming this script is on the player)
    player_colliders = GetComponent<Collider>();



    }

    void Update()
    {
        HandleLook();
        HandleInput();
        HandleInspectRotation();
        MoveHeldObject();
        UpdateVignette();
    }

    /// <summary>
    /// Handles crosshair highlighting and pickup prompt.
    /// </summary>
    void HandleLook()
    {
        if (held_object != null) return;

        Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit hit;
        bool looking_at_pickup = false;

        if (Physics.Raycast(ray, out hit, pickup_range))
        {
            if (hit.collider.CompareTag("Pickup") || hit.collider.CompareTag("Book"))
            {
                looking_at_pickup = true;
                if (interaction_text != null)
                {
                    interaction_text.text = "Press [E] to Pick Up";
                    interaction_text.gameObject.SetActive(true);
                }
            }
        }

        if (!looking_at_pickup && interaction_text != null)
            interaction_text.gameObject.SetActive(false);

        // --- PUZZLE OBJECT INTERACTION HIGHLIGHT ---
        if (held_object == null && Physics.Raycast(ray, out hit, pickup_range))
        {
            if (hit.collider.CompareTag("Interact") || hit.collider.CompareTag("Pedestal"))
            {
                looking_at_pickup = true;

            if (interaction_text != null)
            {
                interaction_text.text = "Press [E] to Interact";
                interaction_text.gameObject.SetActive(true);
            }
            }
        }

        if (reticle != null)
            reticle.color = looking_at_pickup ? highlight_color : normal_color;
    }

    /// <summary>
    /// Handles pickup, drop, inspect, and throw input.
    /// </summary>
    void HandleInput()
    {
        // Try interacting with puzzle objects
        if (Input.GetKeyDown(KeyCode.E) && held_object == null)
        {
            Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
            RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickup_range))
        {
            // 1. Floor board interaction
            var fb = hit.collider.GetComponent<floor_board_controller>();
            if (fb != null)
            {
                fb.OnInteract();
                return;
            }

            // 2. Door interaction
            var door = hit.collider.GetComponent<door_lock_controller>();
            if (door != null)
            {
                door.OnInteract();
                return;
            }

            // 3. Pedestal interaction (place held item)
            var ped = hit.collider.GetComponent<pedestal_controller>();
            if (ped != null)
            {
                ped.OnInteract();
                return;
            }
        }
}

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (held_object == null)
            {
                TryPickup();
            }
            else
            {
                if (is_inspecting)
                {
                    ExitInspectMode();
                    UpdateUIText("Press [E] to Inspect | [Q] to Drop | [Right Click] to Throw");
                }
                else
                {
                    EnterInspectMode();
                    UpdateUIText("Press [E] to Stop Inspecting | [Q] to Drop");
                }

            
            }
        }

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

    /// <summary>
    /// Attempts to pick up an object.
    /// </summary>
    void TryPickup()
    {
        Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickup_range))
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

                UpdateUIText("Press [E] to Inspect | [Q] to Drop | [Right Click] to Throw");

                
                // Disable collisions between all player colliders and held object colliders
            Collider[] player_colliders = GetComponentsInChildren<Collider>();
            Collider[] object_colliders = held_object.GetComponentsInChildren<Collider>();

        foreach (var pc in player_colliders)
    {
        foreach (var oc in object_colliders)
            Physics.IgnoreCollision(pc, oc, true);
    }


            }
        }
    }

    /// <summary>
    /// Moves held object either in front of camera or at inspect position.
    /// Includes bobbing and anti-clipping for inspect mode.
    /// </summary>
    void MoveHeldObject()
    {
        if (held_object == null) return;

        if (!is_inspecting)
        {
            // Normal hold: follow hold point smoothly with bob
            bob_timer += Time.deltaTime * bob_speed;
            float bob_offset = Mathf.Sin(bob_timer) * bob_amplitude;
            Vector3 target_position = hold_point.position + new Vector3(0, bob_offset, 0);

            held_object.transform.position = Vector3.Lerp(
                held_object.transform.position,
                target_position,
                Time.deltaTime * move_force * 0.01f
            );
        }
        else
        {
            // Inspect mode: prevent clipping by raycasting between camera and inspect point
            Vector3 desired_position = inspect_point.position - main_cam.transform.forward * inspect_distance_offset;
            Vector3 cam_to_desired = desired_position - main_cam.transform.position;

            RaycastHit hit;
            float max_distance = cam_to_desired.magnitude;

            if (Physics.Raycast(main_cam.transform.position, cam_to_desired.normalized, out hit, max_distance, ~0, QueryTriggerInteraction.Ignore))
            {
                // Place slightly before surface to avoid clipping
                desired_position = hit.point - cam_to_desired.normalized * 0.05f;
            }

            held_object.transform.position = Vector3.Lerp(
                held_object.transform.position,
                desired_position,
                Time.deltaTime * inspect_zoom_speed
            );

            // Smooth camera zoom while inspecting
            main_cam.fieldOfView = Mathf.Lerp(
                main_cam.fieldOfView,
                zoomed_fov,
                Time.deltaTime * inspect_zoom_speed
            );
        }
    }

    /// <summary>
    /// Rotates object based on mouse input while inspecting.
    /// </summary>
    void HandleInspectRotation()
    {
        if (held_object == null || !is_inspecting) return;

        float mouse_x = Input.GetAxis("Mouse X") * rotation_speed * Time.deltaTime;
        float mouse_y = Input.GetAxis("Mouse Y") * rotation_speed * Time.deltaTime;

        held_object.transform.Rotate(main_cam.transform.up, -mouse_x, Space.World);
        held_object.transform.Rotate(main_cam.transform.right, mouse_y, Space.World);
    }

    /// <summary>
    /// Enter inspect mode: disable player controls, unlock cursor.
    /// </summary>
    void EnterInspectMode()
    {
        is_inspecting = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (player_movement_script != null)
            player_movement_script.enabled = false;

        if (player_camera_script != null)
            player_camera_script.enabled = false;
    }

    /// <summary>
    /// Exit inspect mode: re-enable movement, restore FOV.
    /// </summary>
    void ExitInspectMode()
    {
        is_inspecting = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        if (player_movement_script != null)
            player_movement_script.enabled = true;

        if (player_camera_script != null)
            player_camera_script.enabled = true;

        if (held_object != null)
        {
            StartCoroutine(MoveObjectSmoothly(
                held_object.transform,
                hold_point.position + Vector3.up * post_inspect_lift,
                lift_duration
            ));
        }
    }

    IEnumerator MoveObjectSmoothly(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            obj.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.position = target;
    }

    /// <summary>
    /// Drops currently held object, re-enables physics.
    /// </summary>
    void DropObject()
    {
        if (held_object_rb != null)
        {
            held_object_rb.isKinematic = false;
            held_object_rb.useGravity = true;
        }

        held_object = null;
        held_object_rb = null;
        is_inspecting = false;

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

        // Re-enable collisions between player and dropped object
        Collider[] player_colliders = GetComponentsInChildren<Collider>();
        Collider[] object_colliders = held_object.GetComponentsInChildren<Collider>();

    foreach (var pc in player_colliders)
    {
        foreach (var oc in object_colliders)
            Physics.IgnoreCollision(pc, oc, false);
    }



    }

    /// <summary>
    /// Throws the currently held object forward with force.
    /// </summary>
    void ThrowObject()
    {
        if (held_object_rb == null) return;

        held_object_rb.isKinematic = false;
        held_object_rb.useGravity = true;
        held_object_rb.AddForce(main_cam.transform.forward * throw_force, ForceMode.VelocityChange);

        held_object = null;
        held_object_rb = null;

        is_inspecting = false;

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
        if (held_object_rb != null)
        {
            held_object_rb.isKinematic = false;
            held_object_rb.useGravity = true;
        }

        held_object = null;
        held_object_rb = null;
    }





    /// <summary>
    /// Smoothly updates vignette alpha when inspecting.
    /// </summary>
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
