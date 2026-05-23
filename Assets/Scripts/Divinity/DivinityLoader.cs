using UnityEngine;

[DisallowMultipleComponent]
public class DivinityLoader : MonoBehaviour
{
    [Tooltip("Catalogue de fallback si DivinitySelection.Selected est null (ex. test direct de scène).")]
    public DivinityCatalog catalog;

    [Tooltip("Si renseigné, force cette divinité au lieu d'utiliser DivinitySelection.Selected. Utile pour test.")]
    public DivinityData overrideDivinity;

    [Tooltip("Joue l'application au Start. Si tu préfères contrôler manuellement, décoche et appelle Apply() toi-même.")]
    public bool applyOnStart = true;

    PlayerStats playerStats;
    SpellCaster spellCaster;

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        spellCaster = GetComponent<SpellCaster>();
    }

    void Start()
    {
        if (applyOnStart) Apply();
    }

    public void Apply()
    {
        DivinityData d = overrideDivinity != null ? overrideDivinity : DivinitySelection.GetOrFallback(catalog);
        if (d == null)
        {
            Debug.LogWarning("[DivinityLoader] Aucune divinité sélectionnée et aucun catalog/fallback. Skip.");
            return;
        }

        Debug.Log($"[DivinityLoader] Apply '{d.displayName}'");

        ApplyStats(d);
        ApplySpells(d);
    }

    void ApplyStats(DivinityData d)
    {
        if (playerStats == null) return;
        playerStats.HP += d.bonusHP;
        playerStats.Strength += d.bonusStrength;
        playerStats.HP_multiplier += d.bonusHPMultiplier;
        playerStats.Strength_multiplier += d.bonusStrengthMultiplier;
        if (d.startBlood > 0f) playerStats.Blood = d.startBlood;
    }

    void ApplySpells(DivinityData d)
    {
        if (spellCaster == null) return;

        spellCaster.projectileSpells.Clear();
        spellCaster.projectileSpells.AddRange(d.projectileSpells);
        spellCaster.groundSpells.Clear();
        spellCaster.groundSpells.AddRange(d.groundSpells);

        spellCaster.defaultProjectileIndex = Mathf.Clamp(d.defaultProjectileIndex, 0, Mathf.Max(0, d.projectileSpells.Count - 1));
        spellCaster.defaultGroundIndex = Mathf.Clamp(d.defaultGroundIndex, 0, Mathf.Max(0, d.groundSpells.Count - 1));

        spellCaster.RebuildUnlockState(d.unlockAllSpells);
    }
}
