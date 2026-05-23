using UnityEngine;

public enum ObjectiveType
{
    KillEnemies,
    InteractWithObjects,
    CollectItems,
    Custom,
}

[CreateAssetMenu(menuName = "Einerjahr/Objectives/Objective", fileName = "Objective_New")]
public class ObjectiveData : ScriptableObject
{
    [Header("Identité")]
    public string displayName = "Tuer 10 ennemis";
    [TextArea] public string description = "Élimine tous les ennemis dans la zone.";

    [Header("Type")]
    public ObjectiveType type = ObjectiveType.KillEnemies;
    [Tooltip("Cible à atteindre (nombre de kills, interactions, items, etc.).")]
    [Min(1)] public int targetCount = 10;

    [Header("Filtre (optionnel)")]
    [Tooltip("Si non vide, seuls les évènements taggés avec cet ID comptent. Permet plusieurs objectifs en parallèle (ex. 'BossSquelette').")]
    public string filterTag = "";

    [Header("Récompenses")]
    [Tooltip("XP de compte donnée à la complétion.")]
    public int accountXpReward = 50;
    [Tooltip("Mythril donné à la complétion.")]
    public int mythrilReward = 20;
}
