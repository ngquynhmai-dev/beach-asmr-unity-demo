using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class WinterPlayerController : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public float mouseSensitivity = 2.2f;
    public float interactionRange = 3f;

    private CharacterController controller;
    private Transform view;
    private float verticalVelocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        view = Camera.main != null ? Camera.main.transform : transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float forward = Input.GetAxisRaw("Vertical");
        Vector3 movement = (transform.right * horizontal + transform.forward * forward).normalized * moveSpeed;

        if (controller.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        controller.Move((movement + Vector3.up * verticalVelocity) * Time.deltaTime);

        float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, yaw, 0f);
        pitch = Mathf.Clamp(pitch - mouseY, -70f, 70f);
        if (view != transform)
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void TryInteract()
    {
        if (view == null) return;
        if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, interactionRange))
        {
            WinterDoor door = hit.collider.GetComponentInParent<WinterDoor>();
            if (door != null) door.Toggle();
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(18, 16, 500, 28), "LỐI VÀO CĂN NHÀ BĂNG  •  WASD: di chuyển  •  E: mở cửa");
        GUI.Label(new Rect(18, 44, 500, 24), "Khám phá căn nhà phủ băng và tìm lối vào.");
    }
}