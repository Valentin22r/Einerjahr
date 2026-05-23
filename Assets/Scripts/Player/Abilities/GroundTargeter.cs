using UnityEngine;

[DisallowMultipleComponent]
public class GroundTargeter : MonoBehaviour
{
    [Tooltip("Indicateur par défaut si SetIndicator n'a pas été appelé.")]
    public GameObject indicatorPrefab;

    [Tooltip("Décalage Y appliqué à l'indicateur (pour éviter le z-fighting avec le sol).")]
    public float indicatorYOffset = 0.05f;

    GameObject instance;
    GameObject currentPrefab;
    float currentScale = 1f;

    public bool IsActive { get; private set; }
    public Vector3 CurrentPoint { get; private set; }

    public void SetIndicator(GameObject prefab, float scale)
    {
        currentScale = scale;

        if (currentPrefab == prefab && instance != null)
        {
            ApplyScale();
            return;
        }

        if (instance != null) Destroy(instance);
        currentPrefab = prefab;
        if (prefab == null) { instance = null; return; }

        instance = Instantiate(prefab);
        instance.name = prefab.name + " (Targeter)";
        instance.SetActive(IsActive);
        ApplyScale();
    }

    public void Begin()
    {
        IsActive = true;
        if (instance == null && indicatorPrefab != null) SetIndicator(indicatorPrefab, currentScale);
        if (instance != null) instance.SetActive(true);
    }

    public void Cancel()
    {
        IsActive = false;
        if (instance != null) instance.SetActive(false);
    }

    public void UpdatePoint(Vector3 worldPoint)
    {
        CurrentPoint = worldPoint;
        if (instance != null)
            instance.transform.position = worldPoint + Vector3.up * indicatorYOffset;
    }

    void ApplyScale()
    {
        if (instance == null) return;
        instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, currentScale);
    }

    void OnDestroy()
    {
        if (instance != null) Destroy(instance);
    }
}
