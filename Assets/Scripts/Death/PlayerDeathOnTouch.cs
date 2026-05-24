using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// À placer sur le Player. Si le Player est touché par un objet d'un layer
/// listé dans `killLayer`, il "meurt" et la scène `sceneOnDeath` est chargée
/// (typiquement le lobby).
///
/// Marche avec CharacterController (OnControllerColliderHit) ET avec
/// Collider trigger (OnTriggerEnter) — au cas où des skeletons utilisent
/// un trigger pour leur hitbox.
/// </summary>
[DisallowMultipleComponent]
public class PlayerDeathOnTouch : MonoBehaviour
{
    [Tooltip("Layers qui tuent le joueur au contact (ex. couche 'Skeleton', 'Enemy', etc.).")]
    public LayerMask killLayer;

    [Tooltip("Scène chargée à la mort.")]
    public string sceneOnDeath = "WeaponUpgrade";

    [Tooltip("Délai (s) avant le chargement de la scène. Met 0 pour instant.")]
    [Min(0f)] public float delayBeforeLoad = 0.3f;

    [Tooltip("Active des logs pour debug.")]
    public bool logHits = false;

    bool dying;

    bool IsKillLayer(int layer) => ((1 << layer) & killLayer.value) != 0;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (dying || hit == null || hit.gameObject == null) return;
        if (!IsKillLayer(hit.gameObject.layer)) return;
        Die(hit.gameObject.name);
    }

    void OnTriggerEnter(Collider other)
    {
        if (dying || other == null) return;
        if (!IsKillLayer(other.gameObject.layer)) return;
        Die(other.gameObject.name);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (dying || collision == null) return;
        if (!IsKillLayer(collision.gameObject.layer)) return;
        Die(collision.gameObject.name);
    }

    void Die(string sourceName)
    {
        dying = true;
        if (logHits) Debug.Log($"[PlayerDeathOnTouch] Killed by '{sourceName}', loading '{sceneOnDeath}' in {delayBeforeLoad}s");
        if (delayBeforeLoad <= 0f) LoadScene();
        else Invoke(nameof(LoadScene), delayBeforeLoad);
    }

    void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneOnDeath)) return;
        SceneManager.LoadScene(sceneOnDeath);
    }
}
