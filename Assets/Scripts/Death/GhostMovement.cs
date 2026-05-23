using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mouvement de fantôme : vol libre WASD + Espace/Ctrl pour monter/descendre.
/// Désactivé par défaut, activé par PlayerDeathController en état mort.
/// </summary>
[DisallowMultipleComponent]
public class GhostMovement : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float sprintMultiplier = 2f;
    public Transform cameraPivot;

    void Awake()
    {
        enabled = false;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        Vector3 dir = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) dir += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) dir += Vector3.back;
        if (Keyboard.current.aKey.isPressed) dir += Vector3.left;
        if (Keyboard.current.dKey.isPressed) dir += Vector3.right;
        if (Keyboard.current.spaceKey.isPressed) dir += Vector3.up;
        if (Keyboard.current.leftCtrlKey.isPressed) dir += Vector3.down;

        if (dir.sqrMagnitude < 0.0001f) return;

        Transform refT = cameraPivot != null ? cameraPivot : transform;
        Vector3 worldDir = refT.TransformDirection(dir.normalized);

        float speed = moveSpeed * (Keyboard.current.leftShiftKey.isPressed ? sprintMultiplier : 1f);
        transform.position += worldDir * speed * Time.deltaTime;
    }
}
