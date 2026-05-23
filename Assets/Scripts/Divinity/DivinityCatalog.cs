using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Divinity/Catalog", fileName = "DivinityCatalog")]
public class DivinityCatalog : ScriptableObject
{
    [Tooltip("Liste de toutes les divinités sélectionnables dans le lobby.")]
    public List<DivinityData> divinities = new List<DivinityData>();
    [Tooltip("Divinité par défaut si le joueur n'en a pas choisi (ou si la scène de jeu est lancée directement).")]
    public DivinityData defaultDivinity;
}
