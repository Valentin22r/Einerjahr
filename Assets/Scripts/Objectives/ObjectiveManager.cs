using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton de scène. Gère la liste d'objectifs actifs, leur progression
/// et le déclenchement des actions de complétion (récompenses, transition
/// vers endgame).
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Objectifs de la run")]
    public List<ObjectiveData> objectives = new List<ObjectiveData>();

    [Header("Endgame")]
    [Tooltip("HordeSpawner à démarrer quand tous les objectifs sont complétés. Optionnel.")]
    public HordeSpawner endgameHorde;

    [Header("HUD")]
    public bool showHud = true;
    public int hudFontSize = 16;
    public Color hudColor = new Color(1f, 1f, 1f, 0.95f);
    public Color hudColorDone = new Color(0.4f, 1f, 0.5f, 0.95f);

    [Header("Events")]
    public UnityEvent OnAllObjectivesCompleted;

    readonly Dictionary<ObjectiveData, int> progress = new Dictionary<ObjectiveData, int>();
    bool allCompleted;

    public bool AllCompleted => allCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        foreach (var o in objectives) if (o != null && !progress.ContainsKey(o)) progress[o] = 0;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Incrémente la progression d'un type d'objectif. Filtre via tag optionnel.
    /// </summary>
    public void ReportProgress(ObjectiveType type, int amount = 1, string filterTag = "")
    {
        if (allCompleted) return;
        bool any = false;
        foreach (var o in objectives)
        {
            if (o == null || o.type != type) continue;
            if (!string.IsNullOrEmpty(o.filterTag) && o.filterTag != filterTag) continue;

            int p = progress.TryGetValue(o, out var v) ? v : 0;
            int before = p;
            p = Mathf.Min(o.targetCount, p + amount);
            progress[o] = p;
            any = true;

            if (before < o.targetCount && p >= o.targetCount)
                OnObjectiveCompleted(o);
        }
        if (any) CheckAllCompleted();
    }

    public int GetProgress(ObjectiveData o) => progress.TryGetValue(o, out var v) ? v : 0;

    public bool IsCompleted(ObjectiveData o) => GetProgress(o) >= o.targetCount;

    void OnObjectiveCompleted(ObjectiveData o)
    {
        Debug.Log($"[ObjectiveManager] Completed '{o.displayName}'");
        if (o.accountXpReward > 0) AccountProgress.GrantXp(o.accountXpReward);
        if (o.mythrilReward > 0) PlayerProgress.GrantMythril(o.mythrilReward);
    }

    void CheckAllCompleted()
    {
        foreach (var o in objectives)
        {
            if (o == null) continue;
            if (!IsCompleted(o)) return;
        }
        if (allCompleted) return;
        allCompleted = true;
        Debug.Log("[ObjectiveManager] ALL OBJECTIVES COMPLETED");
        OnAllObjectivesCompleted?.Invoke();
        if (endgameHorde != null) endgameHorde.StartHorde();
    }

    void OnGUI()
    {
        if (!showHud || objectives.Count == 0) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = hudFontSize,
            normal = { textColor = hudColor },
        };
        var styleDone = new GUIStyle(style) { normal = { textColor = hudColorDone } };

        const float w = 320f;
        float lineH = hudFontSize * 1.7f;
        float x = Screen.width - w - 12f;
        float y = 48f;

        GUI.Label(new Rect(x, y - lineH - 4f, w, lineH),
            allCompleted ? "OBJECTIFS COMPLÉTÉS — direction endgame !" : "OBJECTIFS",
            new GUIStyle(style) { fontStyle = FontStyle.Bold, fontSize = hudFontSize + 2 });

        foreach (var o in objectives)
        {
            if (o == null) continue;
            int p = GetProgress(o);
            bool done = p >= o.targetCount;
            string text = $"{(done ? "✓" : "•")} {o.displayName}  ({p}/{o.targetCount})";
            GUI.Label(new Rect(x, y, w, lineH), text, done ? styleDone : style);
            y += lineH;
        }
    }
}
