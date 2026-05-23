using UnityEngine;

/// <summary>
/// À placer sur le Player. Applique les bonus de bénédiction au PlayerStats au Start
/// (en plus de la divinité).
/// </summary>
[DisallowMultipleComponent]
public class BlessingLoader : MonoBehaviour
{
    public BlessingCatalog catalog;
    public bool applyOnStart = true;

    PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        if (applyOnStart) Apply();
    }

    public void Apply()
    {
        if (stats == null || catalog == null) return;
        foreach (var b in catalog.blessings)
        {
            int lvl = BlessingProgress.GetLevel(b);
            if (lvl <= 0) continue;
            stats.HP += b.bonusHpPerLevel * lvl;
            stats.Strength += b.bonusStrengthPerLevel * lvl;
            stats.Blood += b.bonusStartBloodPerLevel * lvl;
            stats.HP_multiplier += b.bonusHpMultiplierPerLevel * lvl;
            stats.Strength_multiplier += b.bonusStrengthMultiplierPerLevel * lvl;
        }
    }
}
