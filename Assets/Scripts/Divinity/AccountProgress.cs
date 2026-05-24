using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Progression de compte globale, persistante entre runs/scènes via PlayerPrefs.
/// Niveau calculé par seuils croissants (level² × baseXp).
/// </summary>
public static class AccountProgress
{
    const string K_Xp = "Einerjahr.Account.Xp";
    const string K_BlessingXp = "Einerjahr.Account.BlessingXp";

    public const int BaseXpForLevel = 100;
    public const int MaxLevel = 50;

    public static event System.Action OnXpChanged;
    public static event System.Action<int> OnLevelUp; // arg = new level

    public static int TotalXp
    {
        get => PlayerPrefs.GetInt(K_Xp, 0);
        private set { PlayerPrefs.SetInt(K_Xp, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int BlessingXp
    {
        get => PlayerPrefs.GetInt(K_BlessingXp, 0);
        set { PlayerPrefs.SetInt(K_BlessingXp, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int Level => LevelFromXp(TotalXp);

    public static int XpForLevel(int level) => level * level * BaseXpForLevel;

    public static int LevelFromXp(int xp)
    {
        int lvl = 0;
        while (lvl < MaxLevel && xp >= XpForLevel(lvl + 1)) lvl++;
        return lvl;
    }

    public static int XpIntoCurrentLevel
    {
        get
        {
            int lvl = Level;
            return TotalXp - XpForLevel(lvl);
        }
    }

    public static int XpNeededForNextLevel
    {
        get
        {
            int lvl = Level;
            if (lvl >= MaxLevel) return 0;
            return XpForLevel(lvl + 1) - XpForLevel(lvl);
        }
    }

    public static void GrantXp(int amount)
    {
        if (amount <= 0) return;
        int before = Level;
        TotalXp += amount;
        BlessingXp += amount; // mêmes points utilisables pour les bénédictions
        OnXpChanged?.Invoke();
        int after = Level;
        if (after > before)
        {
            for (int l = before + 1; l <= after; l++) OnLevelUp?.Invoke(l);
            Debug.Log($"[AccountProgress] Level up ! {before} → {after} (xp={TotalXp})");
        }
    }

    public static bool TrySpendBlessingXp(int cost)
    {
        if (BlessingXp < cost) return false;
        BlessingXp -= cost;
        return true;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(K_Xp);
        PlayerPrefs.DeleteKey(K_BlessingXp);
        PlayerPrefs.Save();
        OnXpChanged?.Invoke();
    }
}
