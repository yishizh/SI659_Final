using UnityEngine;

/// <summary>
/// Temporary desktop fly camera for testing without a VR headset.
/// Attach to Main Camera. Remove or disable before building for Quest.
/// Controls: WASD = move, Mouse right-click drag = look, Q/E = down/up
/// </summary>
public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;
    public float sprintMultiplier = 3f;

    float _yaw;
    float _pitch;

    void Start()
    {
        _yaw   = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        // Look with right mouse button held
        if (Input.GetMouseButton(1))
        {
            _yaw   += Input.GetAxis("Mouse X") * lookSpeed;
            _pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
            _pitch  = Mathf.Clamp(_pitch, -89f, 89f);
            transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
        }

        // Move
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += transform.forward;
        if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
        if (Input.GetKey(KeyCode.A)) dir -= transform.right;
        if (Input.GetKey(KeyCode.D)) dir += transform.right;
        if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) dir += Vector3.down;

        transform.position += dir * speed * Time.deltaTime;
    }
}
