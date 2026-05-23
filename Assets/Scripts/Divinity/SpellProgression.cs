using UnityEngine;

/// <summary>
/// Compte les kills par spell et calcule les bonus de stats. Stockage via
/// PlayerPrefs, persistant entre runs et entre scènes. Clé = nom de l'asset
/// ScriptableObject (ProjectileSpellData.name ou GroundSpellData.name).
/// </summary>
public static class SpellProgression
{
    const string K_Prefix = "Einerjahr.SpellKills.";

    [Tooltip("Niveau += 1 tous les N kills.")]
    public const int KillsPerLevel = 5;

    public const float DamageBonusPerLevel = 0.10f;   // +10% par niveau
    public const float RadiusBonusPerLevel = 0.05f;   // +5% par niveau
    public const float CooldownReductionPerLevel = 0.02f; // -2% par niveau
    public const float MinCooldownMultiplier = 0.3f;  // jamais < 30% du cd de base

    public static int GetKills(string spellId)
    {
        if (string.IsNullOrEmpty(spellId)) return 0;
        return PlayerPrefs.GetInt(K_Prefix + spellId, 0);
    }

    public static void GrantKill(string spellId)
    {
        if (string.IsNullOrEmpty(spellId)) return;
        int before = GetKills(spellId);
        int after = before + 1;
        PlayerPrefs.SetInt(K_Prefix + spellId, after);
        PlayerPrefs.Save();

        int beforeLevel = LevelFromKills(before);
        int afterLevel = LevelFromKills(after);
        if (afterLevel > beforeLevel)
            Debug.Log($"[SpellProgression] '{spellId}' level up → L{afterLevel} (kills {after})");
    }

    public static int LevelFromKills(int kills) => Mathf.Max(0, kills / KillsPerLevel);

    public static int GetLevel(string spellId) => LevelFromKills(GetKills(spellId));

    public static int KillsToNextLevel(string spellId)
    {
        int kills = GetKills(spellId);
        int nextThreshold = (LevelFromKills(kills) + 1) * KillsPerLevel;
        return nextThreshold - kills;
    }

    public static float DamageMultiplier(int level) => 1f + DamageBonusPerLevel * level;
    public static float RadiusMultiplier(int level) => 1f + RadiusBonusPerLevel * level;
    public static float CooldownMultiplier(int level) => Mathf.Max(MinCooldownMultiplier, 1f - CooldownReductionPerLevel * level);

    public static void ResetAll()
    {
        // PlayerPrefs ne permet pas de lister les clés. Reset partiel impossible
        // sans tracker côté code. Pour l'instant on n'expose pas ResetAll global.
        // À la place : reset par spell connu.
    }

    public static void Reset(string spellId)
    {
        if (string.IsNullOrEmpty(spellId)) return;
        PlayerPrefs.DeleteKey(K_Prefix + spellId);
        PlayerPrefs.Save();
    }
}
