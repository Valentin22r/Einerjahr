using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// À placer sur un objet du monde avec un Collider en Trigger.
/// Au passage du joueur (et touche d'interaction), rapporte la progression à l'ObjectiveManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ObjectiveInteractable : MonoBehaviour
{
    [Tooltip("Type d'évènement rapporté à l'ObjectiveManager.")]
    public ObjectiveType reportAs = ObjectiveType.InteractWithObjects;

    [Tooltip("Tag de filtre (optionnel — doit matcher ObjectiveData.filterTag pour compter).")]
    public string filterTag = "";

    [Tooltip("Mode collect : pas de touche, juste passer dessus.")]
    public bool autoTrigger = false;

    [Tooltip("Touche d'interaction (si autoTrigger est faux).")]
    public Key interactionKey = Key.E;

    [Tooltip("Le GameObject est détruit après interaction.")]
    public bool destroyOnUse = true;

    [Tooltip("Texte du prompt affiché quand le joueur est dans la zone.")]
    public string promptText = "Interagir";

    [Header("Affichage")]
    public bool showPrompt = true;
    public int promptFontSize = 16;

    bool playerInRange;
    bool consumed;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (other.GetComponentInParent<PlayerStats>() == null) return;
        playerInRange = true;
        if (autoTrigger) DoTrigger();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null) return;
        playerInRange = false;
    }

    void Update()
    {
        if (autoTrigger || consumed || !playerInRange) return;
        if (Keyboard.current != null && Keyboard.current[interactionKey].wasPressedThisFrame)
            DoTrigger();
    }

    void DoTrigger()
    {
        if (consumed) return;
        consumed = true;
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportProgress(reportAs, 1, filterTag);
        if (destroyOnUse) Destroy(gameObject);
    }

    void OnGUI()
    {
        if (!showPrompt || autoTrigger || consumed || !playerInRange) return;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = promptFontSize,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 1f, 0.6f, 0.95f) }
        };
        GUI.Label(new Rect(0, Screen.height * 0.65f, Screen.width, 28f),
            $"[{interactionKey}] {promptText}", style);
    }
}
