using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// À placer sur un objet du monde (statue, autel) avec un Collider en Trigger.
/// Si un joueur mort est dans la zone, prompt s'affiche, touche d'interaction ressuscite.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReviveStatue : MonoBehaviour
{
    [Tooltip("Rayon de détection des joueurs morts.")]
    public float detectionRadius = 5f;

    [Tooltip("Touche d'interaction (par défaut E).")]
    public Key interactionKey = Key.E;

    [Tooltip("HP restitué à la résurrection.")]
    public float reviveHP = 50f;

    [Tooltip("Délai (s) entre deux résurrections — anti-spam.")]
    public float cooldown = 3f;

    [Header("Affichage")]
    public bool showPrompt = true;
    public Color promptColor = new Color(1f, 1f, 1f, 0.9f);
    public int promptFontSize = 18;

    float nextReviveTime;
    PlayerStats deadInRange;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        deadInRange = FindNearestDeadPlayer();
        if (deadInRange == null) return;

        if (Time.time < nextReviveTime) return;

        if (Keyboard.current != null && Keyboard.current[interactionKey].wasPressedThisFrame)
        {
            deadInRange.Revive(reviveHP);
            nextReviveTime = Time.time + cooldown;
        }
    }

    PlayerStats FindNearestDeadPlayer()
    {
        var hits = Physics.OverlapSphere(transform.position, detectionRadius);
        PlayerStats closest = null;
        float closestSqr = float.MaxValue;
        foreach (var h in hits)
        {
            var ps = h.GetComponentInParent<PlayerStats>();
            if (ps == null || !ps.IsDead) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < closestSqr) { closest = ps; closestSqr = d; }
        }
        return closest;
    }

    void OnGUI()
    {
        if (!showPrompt || deadInRange == null) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = promptFontSize,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = promptColor },
            fontStyle = FontStyle.Bold
        };
        string msg = $"[{interactionKey}] Réanimer {deadInRange.name}";
        GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, 30f), msg, style);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
