using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// À placer sur le Player. À la mort de PlayerStats :
/// - désactive les Behaviour listés (mouvement normal, combat, spells, etc.)
/// - active le Behaviour 'ghost' (mode fantôme : vol libre, immunité collision ennemis)
/// - rend le Player détectable par les ReviveStatue
/// À la résurrection : remet tout dans l'état initial.
/// </summary>
[DisallowMultipleComponent]
public class PlayerDeathController : MonoBehaviour
{
    [Tooltip("Components désactivés en état mort (mouvement, combat, spells…).")]
    public List<Behaviour> componentsDisabledOnDeath = new List<Behaviour>();

    [Tooltip("Component activé en état mort (ex. GhostMovement). Optionnel.")]
    public Behaviour ghostComponent;

    [Tooltip("Rigidbody du player. Mis en kinematic en ghost mode pour traverser les obstacles.")]
    public Rigidbody playerRigidbody;

    [Tooltip("Mesh/Renderer désactivés en ghost mode (le corps disparaît).")]
    public List<Renderer> renderersHiddenOnDeath = new List<Renderer>();

    [Tooltip("Layer appliqué au Player en ghost mode (ex. 'Ignore Raycast' pour ne plus collider avec les ennemis).")]
    public int ghostLayer = 2; // Ignore Raycast

    PlayerStats stats;
    int originalLayer;
    bool isGhost;

    public bool IsGhost => isGhost;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        originalLayer = gameObject.layer;
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnPlayerDeath.AddListener(EnterGhostMode);
            stats.OnPlayerRevived.AddListener(ExitGhostMode);
        }
    }

    void OnDisable()
    {
        if (stats != null)
        {
            stats.OnPlayerDeath.RemoveListener(EnterGhostMode);
            stats.OnPlayerRevived.RemoveListener(ExitGhostMode);
        }
    }

    public void EnterGhostMode()
    {
        if (isGhost) return;
        isGhost = true;

        foreach (var c in componentsDisabledOnDeath) if (c != null) c.enabled = false;
        if (ghostComponent != null) ghostComponent.enabled = true;
        if (playerRigidbody != null) { playerRigidbody.linearVelocity = Vector3.zero; playerRigidbody.isKinematic = true; }
        foreach (var r in renderersHiddenOnDeath) if (r != null) r.enabled = false;
        gameObject.layer = ghostLayer;

        Debug.Log("[PlayerDeath] Entered ghost mode");
    }

    public void ExitGhostMode()
    {
        if (!isGhost) return;
        isGhost = false;

        foreach (var c in componentsDisabledOnDeath) if (c != null) c.enabled = true;
        if (ghostComponent != null) ghostComponent.enabled = false;
        if (playerRigidbody != null) playerRigidbody.isKinematic = false;
        foreach (var r in renderersHiddenOnDeath) if (r != null) r.enabled = true;
        gameObject.layer = originalLayer;

        Debug.Log("[PlayerDeath] Exited ghost mode");
    }
}
