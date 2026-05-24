using UnityEngine;

/// <summary>
/// Trigger qui démarre la HordeSpawner quand un joueur entre dans la zone.
/// Place-le à l'endroit du "boss arena" / endgame de la map.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HordeTrigger : MonoBehaviour
{
    [Tooltip("Horde à démarrer quand un joueur entre dans le trigger.")]
    public HordeSpawner horde;

    [Tooltip("Le trigger ne s'active qu'une fois.")]
    public bool oneShot = true;

    bool triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && triggered) return;
        if (other.GetComponentInParent<PlayerStats>() == null) return;
        triggered = true;
        if (horde != null) horde.StartHorde();
    }
}
