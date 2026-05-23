using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Spells/Ground Spell", fileName = "GroundSpell_New")]
public class GroundSpellData : ScriptableObject
{
    [Header("Identité")]
    public string displayName = "Thunderstorm";

    [Header("Progression")]
    [Tooltip("Si vrai, le spell démarre verrouillé. Appeler SpellCaster.UnlockGround pour le débloquer.")]
    public bool startsLocked = false;

    [Header("Prefabs")]
    [Tooltip("Prefab AoE persistant (Spells Pack / Spell_Storm, etc.).")]
    public GameObject aoeEffectPrefab;
    [Tooltip("Indicateur visuel pendant la visée. Si vide, l'indicateur par défaut du SpellCaster est utilisé.")]
    public GameObject indicatorPrefab;

    [Header("Stats")]
    [Min(0.1f)] public float radius = 4f;
    [Min(0.1f)] public float duration = 4f;
    [Min(0.05f)] public float tickInterval = 0.5f;
    [Min(0f)] public float damagePerTick = 15f;
    [Min(0f)] public float cooldown = 2f;

    [Header("Échelles visuelles (1 = taille du prefab)")]
    [Tooltip("Échelle appliquée au prefab d'effet AoE.")]
    [Min(0.01f)] public float effectScale = 1f;
    [Tooltip("Échelle appliquée à l'indicateur. Si <= 0, l'échelle est auto-calculée à partir du rayon.")]
    public float indicatorScale = -1f;

    [Header("Collisions")]
    public LayerMask targetMask = ~0;

    [Header("Critique (chance par cast)")]
    [Range(0f, 1f)] public float critChance = 0.01f;
    [Min(1f)] public float critDamageMultiplier = 5f;
    [Tooltip("Multiplie le rayon de dégâts en crit.")]
    [Min(1f)] public float critRadiusMultiplier = 10f;
    [Tooltip("Multiplie l'échelle visuelle de l'effet et de l'indicateur en crit.")]
    [Min(1f)] public float critScaleMultiplier = 10f;
}
