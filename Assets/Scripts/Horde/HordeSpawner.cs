using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner d'horde infinie. Démarre à la demande (StartHorde()) ou au Start.
/// Plus la vague avance, plus :
///   - les ennemis spawnent vite
///   - leur HP augmente
///   - leurs dégâts augmentent
/// Un cap de mobs vivants en simultané protège les perfs.
/// </summary>
public class HordeSpawner : MonoBehaviour
{
    [Header("Prefabs ennemis")]
    [Tooltip("Pool d'ennemis à spawn. Un random est choisi à chaque spawn.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("Points de spawn")]
    [Tooltip("Transforms autour desquels les ennemis apparaissent. Si vide, spawn autour de transform.position.")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("Rayon aléatoire autour d'un spawn point.")]
    public float spawnSpread = 2f;

    [Header("Cadence")]
    [Tooltip("Intervalle initial entre deux spawns (s).")]
    public float startInterval = 3f;
    [Tooltip("Intervalle min après scaling.")]
    public float minInterval = 0.4f;
    [Tooltip("Réduction d'intervalle par minute écoulée.")]
    public float intervalDecreasePerMinute = 0.5f;

    [Header("Scaling difficulté")]
    [Tooltip("Multiplicateur HP par minute écoulée (+50% / min).")]
    public float hpScalePerMinute = 0.5f;
    [Tooltip("Multiplicateur damage par minute écoulée (+30% / min).")]
    public float damageScalePerMinute = 0.3f;
    [Tooltip("Bonus de Mythril drop par minute écoulée (+1 / min).")]
    public int mythrilDropBonusPerMinute = 1;

    [Header("Performance")]
    [Tooltip("Cap d'ennemis vivants en même temps. Au-delà, plus de spawn jusqu'à ce que certains meurent.")]
    public int maxAliveEnemies = 60;

    [Header("Lancement")]
    [Tooltip("Démarre automatiquement la horde au Start.")]
    public bool autoStart = false;

    [Header("HUD")]
    public bool showHud = true;

    public bool IsRunning { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int TotalSpawned { get; private set; }

    readonly List<EnemyStats> aliveTracking = new List<EnemyStats>();
    float nextSpawnTime;

    void Start()
    {
        if (autoStart) StartHorde();
    }

    public void StartHorde()
    {
        IsRunning = true;
        ElapsedSeconds = 0f;
        TotalSpawned = 0;
        nextSpawnTime = Time.time + 1f;
        Debug.Log("[HordeSpawner] Horde started");
    }

    public void StopHorde()
    {
        IsRunning = false;
        Debug.Log($"[HordeSpawner] Horde stopped — survived {ElapsedSeconds:F0}s, {TotalSpawned} mobs");
    }

    void Update()
    {
        if (!IsRunning) return;
        ElapsedSeconds += Time.deltaTime;

        // purge dead refs
        for (int i = aliveTracking.Count - 1; i >= 0; i--)
            if (aliveTracking[i] == null) aliveTracking.RemoveAt(i);

        if (aliveTracking.Count >= maxAliveEnemies) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnOne();
            nextSpawnTime = Time.time + CurrentInterval();
        }
    }

    float CurrentInterval()
    {
        float minutes = ElapsedSeconds / 60f;
        return Mathf.Max(minInterval, startInterval - intervalDecreasePerMinute * minutes);
    }

    float CurrentHPScale()
    {
        float minutes = ElapsedSeconds / 60f;
        return 1f + hpScalePerMinute * minutes;
    }

    float CurrentDamageScale()
    {
        float minutes = ElapsedSeconds / 60f;
        return 1f + damageScalePerMinute * minutes;
    }

    int CurrentMythrilBonus()
    {
        float minutes = ElapsedSeconds / 60f;
        return Mathf.RoundToInt(mythrilDropBonusPerMinute * minutes);
    }

    void SpawnOne()
    {
        if (enemyPrefabs.Count == 0) return;
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (prefab == null) return;

        Vector3 pos;
        if (spawnPoints.Count > 0)
        {
            var sp = spawnPoints[Random.Range(0, spawnPoints.Count)];
            pos = sp.position + Random.insideUnitSphere * spawnSpread;
            pos.y = sp.position.y;
        }
        else
        {
            pos = transform.position + Random.insideUnitSphere * spawnSpread;
            pos.y = transform.position.y;
        }

        var go = Instantiate(prefab, pos, Quaternion.identity);
        var stats = go.GetComponent<EnemyStats>();
        if (stats != null)
        {
            stats.HP = Mathf.RoundToInt(stats.HP * CurrentHPScale());
            stats.damage = Mathf.RoundToInt(stats.damage * CurrentDamageScale());
            stats.mythrilDrop += CurrentMythrilBonus();
            aliveTracking.Add(stats);
        }
        TotalSpawned++;
    }

    void OnGUI()
    {
        if (!showHud || !IsRunning) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = new Color(1f, 0.6f, 0.3f, 0.95f) },
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        int mn = Mathf.FloorToInt(ElapsedSeconds / 60f);
        int sc = Mathf.FloorToInt(ElapsedSeconds % 60f);
        GUI.Label(new Rect(0, 10f, Screen.width, 28f),
            $"HORDE — {mn:00}:{sc:00}  |  mobs {aliveTracking.Count}/{maxAliveEnemies}  |  total {TotalSpawned}", style);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (spawnPoints.Count == 0)
            Gizmos.DrawWireSphere(transform.position, spawnSpread);
        else
            foreach (var sp in spawnPoints)
                if (sp != null) Gizmos.DrawWireSphere(sp.position, spawnSpread);
    }
}
