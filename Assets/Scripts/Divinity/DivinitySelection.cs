using UnityEngine;

public static class DivinitySelection
{
    public static DivinityData Selected { get; private set; }

    public static void Select(DivinityData divinity)
    {
        Selected = divinity;
        if (divinity != null) Debug.Log($"[DivinitySelection] Selected '{divinity.displayName}'");
    }

    public static DivinityData GetOrFallback(DivinityCatalog catalog)
    {
        if (Selected != null) return Selected;
        if (catalog != null && catalog.defaultDivinity != null) return catalog.defaultDivinity;
        if (catalog != null && catalog.divinities.Count > 0) return catalog.divinities[0];
        return null;
    }
}
