using UnityEngine;

/// <summary>
/// Niveaux de bénédictions achetés (persistant PlayerPrefs). Le coût se paie
/// avec l'XP de bénédiction (AccountProgress.BlessingXp).
/// </summary>
public static class BlessingProgress
{
    const string K_Prefix = "Einerjahr.Blessing.";

    public static int GetLevel(BlessingData b)
    {
        if (b == null) return 0;
        return PlayerPrefs.GetInt(K_Prefix + b.name, 0);
    }

    public static bool ArePrerequisitesMet(BlessingData b)
    {
        if (b == null) return false;
        foreach (var pre in b.prerequisites)
            if (pre != null && GetLevel(pre) <= 0) return false;
        return true;
    }

    public static bool CanUpgrade(BlessingData b)
    {
        if (b == null) return false;
        if (!ArePrerequisitesMet(b)) return false;
        if (GetLevel(b) >= b.maxLevel) return false;
        return AccountProgress.BlessingXp >= b.xpPerLevel;
    }

    public static bool TryUpgrade(BlessingData b)
    {
        if (!CanUpgrade(b)) return false;
        if (!AccountProgress.TrySpendBlessingXp(b.xpPerLevel)) return false;
        SetLevel(b, GetLevel(b) + 1);
        return true;
    }

    public static void SetLevel(BlessingData b, int level)
    {
        if (b == null) return;
        PlayerPrefs.SetInt(K_Prefix + b.name, Mathf.Clamp(level, 0, b.maxLevel));
        PlayerPrefs.Save();
    }

    public static void Reset(BlessingData b)
    {
        if (b == null) return;
        PlayerPrefs.DeleteKey(K_Prefix + b.name);
        PlayerPrefs.Save();
    }
}
