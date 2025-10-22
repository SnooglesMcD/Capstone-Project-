using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walk_speed = 3f;
    public float run_speed = 6f;
    public float crouch_speed = 1.5f;
    public float gravity = -9.81f;
    public float jump_height = 1.2f;

    [Header("Mouse Look Settings")]
    public float mouse_sensitivity = 2f;
    public Transform player_camera;
    public float look_x_limit = 80f;

    private CharacterController controller;
    private Vector3 velocity;
    private float rotation_x = 0f;
    private bool is_running;
    private bool is_crouching;
    private float original_height;
    private float crouch_height = 1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        original_height = controller.height;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        bool is_grounded = controller.isGrounded;
        if (is_grounded && velocity.y < 0)
            velocity.y = -2f;

        float move_x = Input.GetAxis("Horizontal");
        float move_z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * move_x + transform.forward * move_z;

        is_running = Input.GetKey(KeyCode.LeftShift);
        is_crouching = Input.GetKey(KeyCode.LeftControl);

        float current_speed = walk_speed;
        if (is_running && !is_crouching) current_speed = run_speed;
        else if (is_crouching) current_speed = crouch_speed;

        controller.Move(move * current_speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && is_grounded && !is_crouching)
            velocity.y = Mathf.Sqrt(jump_height * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (is_crouching)
            controller.height = Mathf.Lerp(controller.height, crouch_height, Time.deltaTime * 8f);
        else
            controller.height = Mathf.Lerp(controller.height, original_height, Time.deltaTime * 8f);
    }

    void HandleMouseLook()
    {
        float mouse_x = Input.GetAxis("Mouse X") * mouse_sensitivity;
        float mouse_y = Input.GetAxis("Mouse Y") * mouse_sensitivity;

        rotation_x -= mouse_y;
        rotation_x = Mathf.Clamp(rotation_x, -look_x_limit, look_x_limit);

        player_camera.localRotation = Quaternion.Euler(rotation_x, 0, 0);
        transform.Rotate(Vector3.up * mouse_x);
    }
}
