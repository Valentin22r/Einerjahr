using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Blessing/Catalog", fileName = "BlessingCatalog")]
public class BlessingCatalog : ScriptableObject
{
    public List<BlessingData> blessings = new List<BlessingData>();
}
