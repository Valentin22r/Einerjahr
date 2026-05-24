using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Brutal,
}

/// <summary>
/// Sélection de difficulté persistante (PlayerPrefs) — appliquée par les spawners et l'IA ennemie.
/// </summary>
public static class DifficultySelection
{
    const string K_Difficulty = "Einerjahr.Difficulty";

    public static Difficulty Current
    {
        get => (Difficulty)PlayerPrefs.GetInt(K_Difficulty, (int)Difficulty.Normal);
        set { PlayerPrefs.SetInt(K_Difficulty, (int)value); PlayerPrefs.Save(); }
    }

    /// <summary>Multiplicateur de spawn rate (ennemis spawnent plus vite).</summary>
    public static float SpawnRateMultiplier(Difficulty d) => d switch
    {
        Difficulty.Easy   => 0.5f,
        Difficulty.Normal => 1f,
        Difficulty.Hard   => 1.5f,
        Difficulty.Brutal => 2.5f,
        _                 => 1f,
    };

    /// <summary>Multiplicateur HP ennemis.</summary>
    public static float EnemyHpMultiplier(Difficulty d) => d switch
    {
        Difficulty.Easy   => 0.7f,
        Difficulty.Normal => 1f,
        Difficulty.Hard   => 1.5f,
        Difficulty.Brutal => 2.5f,
        _                 => 1f,
    };

    /// <summary>Multiplicateur damage ennemis.</summary>
    public static float EnemyDamageMultiplier(Difficulty d) => d switch
    {
        Difficulty.Easy   => 0.7f,
        Difficulty.Normal => 1f,
        Difficulty.Hard   => 1.4f,
        Difficulty.Brutal => 2f,
        _                 => 1f,
    };

    /// <summary>Bonus Mythril/XP (récompense pour difficulté plus élevée).</summary>
    public static float RewardMultiplier(Difficulty d) => d switch
    {
        Difficulty.Easy   => 0.8f,
        Difficulty.Normal => 1f,
        Difficulty.Hard   => 1.5f,
        Difficulty.Brutal => 2.5f,
        _                 => 1f,
    };
}
