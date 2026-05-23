using UnityEngine;

/// <summary>
/// Stockage persistant des données de progression entre scènes.
/// Utilise PlayerPrefs — pas de SO/SaveSystem complexe à mettre en place.
/// Sera remplacé par un vrai système de save plus tard.
/// </summary>
public static class PlayerProgress
{
    const string K_Mythril = "Einerjahr.Mythril";
    const string K_WeaponDamageLevel = "Einerjahr.Weapon.DamageLevel";
    const string K_WeaponRangeLevel = "Einerjahr.Weapon.RangeLevel";
    const string K_WeaponSpeedLevel = "Einerjahr.Weapon.SpeedLevel";

    public static int Mythril
    {
        get => PlayerPrefs.GetInt(K_Mythril, 0);
        set { PlayerPrefs.SetInt(K_Mythril, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int WeaponDamageLevel
    {
        get => PlayerPrefs.GetInt(K_WeaponDamageLevel, 1);
        set { PlayerPrefs.SetInt(K_WeaponDamageLevel, Mathf.Max(1, value)); PlayerPrefs.Save(); }
    }

    public static int WeaponRangeLevel
    {
        get => PlayerPrefs.GetInt(K_WeaponRangeLevel, 1);
        set { PlayerPrefs.SetInt(K_WeaponRangeLevel, Mathf.Max(1, value)); PlayerPrefs.Save(); }
    }

    public static int WeaponSpeedLevel
    {
        get => PlayerPrefs.GetInt(K_WeaponSpeedLevel, 1);
        set { PlayerPrefs.SetInt(K_WeaponSpeedLevel, Mathf.Max(1, value)); PlayerPrefs.Save(); }
    }

    public static int CostForLevel(int currentLevel) => currentLevel * 10;

    public static bool TrySpendMythril(int amount)
    {
        if (Mythril < amount) return false;
        Mythril -= amount;
        return true;
    }

    public static void GrantMythril(int amount)
    {
        if (amount <= 0) return;
        Mythril += amount;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() { /* force-load PlayerPrefs */ }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(K_Mythril);
        PlayerPrefs.DeleteKey(K_WeaponDamageLevel);
        PlayerPrefs.DeleteKey(K_WeaponRangeLevel);
        PlayerPrefs.DeleteKey(K_WeaponSpeedLevel);
        PlayerPrefs.Save();
    }
}
