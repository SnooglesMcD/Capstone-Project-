using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PickupController : MonoBehaviour
{
    [Header("References")]
    public Camera main_cam;
    public Transform hold_point;           // Normal hold position in front of camera
    public Transform inspect_point;        // Closer position for inspection

    [Header("Settings")]
    public float pickup_range = 3f;
    public float move_force = 250f;
    public float inspect_zoom_speed = 4f;
    public float rotation_speed = 100f;
    public float post_inspect_lift = 0.3f; // How much the object moves back after inspection
    public float lift_duration = 0.2f;     // Time to move object back smoothly

    [Header("UI Elements")]
    public TextMeshProUGUI interaction_text;
    public Image reticle;
    public Color normal_color = Color.white;
    public Color highlight_color = Color.cyan;

    [Header("Vignette")]
    public Image vignette_image;            // Fullscreen UI Image for darkening
    public float vignette_max_alpha = 0.25f;
    public float vignette_speed = 3f;

    [Header("Hold Bob Settings")]
    public float bob_amplitude = 0.05f;    // Vertical bob amount
    public float bob_speed = 2f;           // Bob oscillation speed

    // Runtime variables
    private GameObject held_object;
    private Rigidbody held_object_rb;
    private bool is_inspecting = false;

    private float original_fov;
    public float zoomed_fov = 40f;

    private Vector3 hold_start_position;    // Original local position for bob
    private float bob_timer = 0f;

    void Start()
    {
        // Hide interaction text at start
        if (interaction_text != null)
            interaction_text.gameObject.SetActive(false);

        // Store the original camera FOV so we can restore it later
        if (main_cam != null)
            original_fov = main_cam.fieldOfView;

        // Make sure vignette starts invisible
        if (vignette_image != null)
        {
            var col = vignette_image.color;
            col.a = 0f;
            vignette_image.color = col;
        }
    }

    void Update()
    {
        HandleLook();       // Highlight pickups & show UI
        HandlePickup();     // Pickup, inspect, drop logic
        HandleInspect();    // Rotate object while inspecting
        MoveHeldObject();   // Smooth movement toward hold or inspect point + bob
        UpdateVignette();   // Fade vignette in/out
    }

    /// <summary>
    /// Highlights pickupable objects and shows interaction prompt.
    /// Prevents showing the prompt for the currently held object.
    /// </summary>
    void HandleLook()
    {
        if (is_inspecting) return; // Skip while inspecting

        Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit hit;
        bool looking_at_pickup = false;

        if (Physics.Raycast(ray, out hit, pickup_range))
        {
            if (hit.collider.CompareTag("Pickup") && hit.collider.gameObject != held_object)
            {
                looking_at_pickup = true;
                if (interaction_text != null)
                    interaction_text.gameObject.SetActive(true);
            }
        }

        if (!looking_at_pickup && interaction_text != null)
            interaction_text.gameObject.SetActive(false);

        if (reticle != null)
            reticle.color = looking_at_pickup ? highlight_color : normal_color;
    }

    /// <summary>
    /// Handles pickup, inspect, and drop via E key.
    /// </summary>
    void HandlePickup()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (held_object == null)
            {
                TryPickup();
            }
            else
            {
                if (is_inspecting)
                    ExitInspectMode();
                else
                    DropObject();
            }
        }
    }

    /// <summary>
    /// Attempts to pick up an object in front of the player.
    /// Applies kinematic fix to prevent flying.
    /// Initializes bob effect.
    /// </summary>
    void TryPickup()
    {
        Ray ray = new Ray(main_cam.transform.position, main_cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickup_range))
        {
            if (hit.collider != null && hit.collider.CompareTag("Pickup"))
            {
                held_object = hit.collider.gameObject;
                held_object_rb = held_object.GetComponent<Rigidbody>();

                if (held_object_rb != null)
                {
                    // Disable gravity and physics while holding
                    held_object_rb.useGravity = false;
                    held_object_rb.isKinematic = true;
                    held_object_rb.linearDamping = 10f;
                    held_object_rb.transform.parent = hold_point;
                }

                if (interaction_text != null)
                    interaction_text.gameObject.SetActive(false);

                // Initialize bob effect
                hold_start_position = held_object.transform.localPosition;
                bob_timer = 0f;

                EnterInspectMode();
            }
        }
    }

    /// <summary>
    /// Moves held object smoothly to hold or inspect point.
    /// Applies bob when holding normally.
    /// Zooms camera if inspecting.
    /// </summary>
    void MoveHeldObject()
    {
        if (held_object == null) return;

        if (!is_inspecting)
        {
            // Normal hold: move object toward hold point
            Vector3 move_direction = hold_point.position - held_object.transform.position;
            held_object_rb.AddForce(move_direction * move_force * Time.deltaTime);

            // Apply subtle vertical bob
            bob_timer += Time.deltaTime * bob_speed;
            float bob_offset = Mathf.Sin(bob_timer) * bob_amplitude;
            Vector3 bob_position = hold_start_position + new Vector3(0, bob_offset, 0);
            held_object.transform.localPosition = bob_position;
        }
        else
        {
            // Inspect mode: smoothly move object to inspect point
            held_object.transform.position = Vector3.Lerp(
                held_object.transform.position,
                inspect_point.position,
                Time.deltaTime * inspect_zoom_speed
            );

            // Smooth camera zoom
            main_cam.fieldOfView = Mathf.Lerp(
                main_cam.fieldOfView,
                zoomed_fov,
                Time.deltaTime * inspect_zoom_speed
            );
        }
    }

    /// <summary>
    /// Rotates held object based on mouse input while inspecting.
    /// </summary>
    void HandleInspect()
    {
        if (held_object == null || !is_inspecting) return;

        float mouse_x = Input.GetAxis("Mouse X") * rotation_speed * Time.deltaTime;
        float mouse_y = Input.GetAxis("Mouse Y") * rotation_speed * Time.deltaTime;

        held_object.transform.Rotate(main_cam.transform.up, -mouse_x, Space.World);
        held_object.transform.Rotate(main_cam.transform.right, mouse_y, Space.World);
    }

    /// <summary>
    /// Enables inspect mode, unlocks cursor, allows rotation.
    /// </summary>
    void EnterInspectMode()
    {
        is_inspecting = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Exits inspect mode.
    /// Smoothly moves object slightly back to avoid flying.
    /// Keeps Rigidbody kinematic while held.
    /// </summary>
    void ExitInspectMode()
    {
        is_inspecting = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Restore camera FOV
        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        // Smooth post-inspect lift/back
        if (held_object != null)
        {
            held_object_rb.isKinematic = true; // Keep physics disabled while holding
            Vector3 target_position = hold_point.position + main_cam.transform.forward * -post_inspect_lift;
            StartCoroutine(MoveObjectSmoothly(held_object.transform, target_position, lift_duration));
        }
    }

    /// <summary>
    /// Coroutine to smoothly move object to a target position over time.
    /// Prevents sudden physics forces that can launch the object.
    /// </summary>
    private IEnumerator MoveObjectSmoothly(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            obj.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.position = target; // Ensure final position is exact
    }

    /// <summary>
    /// Drops the currently held object and re-enables physics.
    /// </summary>
    void DropObject()
    {
        if (held_object == null) return;

        // Re-enable physics
        held_object_rb.isKinematic = false;
        held_object_rb.useGravity = true;
        held_object_rb.linearDamping = 1f;

        held_object.transform.parent = null;
        held_object = null;
        held_object_rb = null;
        is_inspecting = false;

        if (main_cam != null)
            main_cam.fieldOfView = original_fov;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Updates the vignette overlay alpha smoothly based on inspect state.
    /// </summary>
    void UpdateVignette()
    {
        if (vignette_image == null) return;

        float target_alpha = is_inspecting ? vignette_max_alpha : 0f;
        Color col = vignette_image.color;
        col.a = Mathf.Lerp(col.a, target_alpha, Time.deltaTime * vignette_speed);
        vignette_image.color = col;
    }
}
