using UnityEngine;

/// <summary>
/// First-person desktop controller for testing the campus tour without a VR headset.
/// Auto-disables itself if OVRCameraRig is present in the scene.
/// REMOVE or DISABLE this component before building for Quest.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DesktopTestController : MonoBehaviour
{
    [Header("── DESKTOP TESTING ONLY - Remove before VR build ──")]

    [Tooltip("Movement speed in metres per second.")]
    public float moveSpeed = 5f;

    [Tooltip("Mouse look sensitivity.")]
    public float mouseSensitivity = 300f;

    [Tooltip("Arrow key look sensitivity (degrees per second).")]
    public float arrowSensitivity = 90f;

    [Tooltip("Gravity force applied each second.")]
    public float gravity = -9.81f;

    // -------------------------------------------------------------------------
    CharacterController _cc;
    Camera _camera;
    float _xRotation;   // vertical look angle (clamped)
    Vector3 _velocity;  // tracks falling speed

    // -------------------------------------------------------------------------
    void Awake()
    {
        // Auto-disable when OVRCameraRig is present so both rigs can coexist
        // in the same scene during the transition period.
        if (FindObjectOfType<OVRCameraRig>() != null)
        {
            Debug.Log("[DesktopTestController] OVRCameraRig detected — disabling desktop controller.");
            gameObject.SetActive(false);
            return;
        }

        _cc = GetComponent<CharacterController>();

        // Find the child camera (created by the Editor helper or already present)
        _camera = GetComponentInChildren<Camera>();
        if (_camera == null)
        {
            Debug.LogError("[DesktopTestController] No Camera found as a child of DesktopPlayer. " +
                           "Run CampusTour > Setup Desktop Player, or manually add a Camera child " +
                           "at local position (0, 1.7, 0).");
            enabled = false;
            return;
        }

        LockCursor();
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleCursorLock();
    }

    // ── Look (mouse OR arrow keys) ────────────────────────────────────────────
    void HandleLook()
    {
        float lookX = 0f;
        float lookY = 0f;

        // Mouse (requires cursor lock)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            lookX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            lookY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        }

        // Arrow keys always work — no cursor lock needed
        if (Input.GetKey(KeyCode.LeftArrow))  lookX -= arrowSensitivity * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) lookX += arrowSensitivity * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow))    lookY += arrowSensitivity * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))  lookY -= arrowSensitivity * Time.deltaTime;

        _xRotation -= lookY;
        _xRotation  = Mathf.Clamp(_xRotation, -90f, 90f);

        _camera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * lookX);
    }

    // ── WASD movement + gravity ───────────────────────────────────────────────
    void HandleMovement()
    {
        // Reset downward velocity when grounded
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;   // small negative keeps isGrounded reliable

        float h = Input.GetAxis("Horizontal");   // A / D
        float v = Input.GetAxis("Vertical");     // W / S

        Vector3 move = transform.right * h + transform.forward * v;
        _cc.Move(move * moveSpeed * Time.deltaTime);

        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ── Cursor lock / unlock ──────────────────────────────────────────────────
    void HandleCursorLock()
    {
        if (Input.GetMouseButtonDown(0))
            LockCursor();

        if (Input.GetKeyDown(KeyCode.Escape))
            UnlockCursor();
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}
