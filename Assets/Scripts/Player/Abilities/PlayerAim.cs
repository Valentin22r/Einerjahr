using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAim : MonoBehaviour
{
    [Header("Caméra")]
    [Tooltip("Pivot enfant du player à hauteur des yeux. La caméra doit être enfant de ce pivot.")]
    public Transform cameraPivot;
    [Tooltip("Caméra de vue. Si vide, Camera enfant ou Camera.main est utilisée.")]
    public Camera viewCamera;

    [Header("Souris")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    [Tooltip("Lock le curseur au démarrage (FPS classique).")]
    public bool lockCursorOnStart = true;
    [Tooltip("Touche pour libérer / re-verrouiller le curseur.")]
    public Key toggleCursorKey = Key.Escape;

    [Header("Visée")]
    [Tooltip("Couches considérées comme sol pour le mode ciblage AoE.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Distance max du raycast de visée.")]
    public float maxAimDistance = 200f;

    float yaw;
    float pitch;

    public Vector3 AimOrigin => viewCamera != null ? viewCamera.transform.position : transform.position;
    public Vector3 AimDirection => viewCamera != null ? viewCamera.transform.forward : transform.forward;

    public Vector3 AimPoint
    {
        get
        {
            if (Physics.Raycast(AimOrigin, AimDirection, out var info, maxAimDistance, ~0, QueryTriggerInteraction.Ignore))
                return info.point;
            return AimOrigin + AimDirection * maxAimDistance;
        }
    }

    void Awake()
    {
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        if (viewCamera == null) viewCamera = Camera.main;
        if (cameraPivot == null && viewCamera != null) cameraPivot = viewCamera.transform;

        yaw = transform.eulerAngles.y;
        if (cameraPivot != null)
        {
            float p = cameraPivot.localEulerAngles.x;
            pitch = p > 180f ? p - 360f : p;
        }

        if (lockCursorOnStart) SetCursorLocked(true);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleCursorKey].wasPressedThisFrame)
            SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);

        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch -= delta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public bool TryGetGroundPointUnderCursor(out Vector3 point)
    {
        if (Physics.Raycast(AimOrigin, AimDirection, out var info, maxAimDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = info.point;
            return true;
        }
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = new Ray(AimOrigin, AimDirection);
        if (plane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }
        point = AimOrigin + AimDirection * maxAimDistance;
        return false;
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
