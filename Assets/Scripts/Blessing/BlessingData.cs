using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Blessing/Blessing", fileName = "Blessing_New")]
public class BlessingData : ScriptableObject
{
    [Header("Identité")]
    public string displayName = "Bénédiction du combat";
    [TextArea] public string description = "+10 Strength";
    public Sprite icon;

    [Header("Coût et niveau")]
    [Tooltip("XP de bénédiction nécessaire par niveau.")]
    [Min(1)] public int xpPerLevel = 50;
    [Tooltip("Niveau max achetable.")]
    [Min(1)] public int maxLevel = 5;

    [Header("Bonus de stats (multipliés par le niveau acheté)")]
    public float bonusHpPerLevel = 0f;
    public float bonusStrengthPerLevel = 10f;
    public float bonusStartBloodPerLevel = 0f;
    public int bonusHpMultiplierPerLevel = 0;
    public int bonusStrengthMultiplierPerLevel = 0;

    [Header("Prerequis (optionnel)")]
    [Tooltip("Bénédictions qui doivent être à au moins niveau 1 pour pouvoir débloquer celle-ci.")]
    public BlessingData[] prerequisites = new BlessingData[0];
}
