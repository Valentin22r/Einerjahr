using UnityEngine;

public static class BloodSpawner
{
    public static void Spawn(GameObject bloodPrefab, Vector3 point, Vector3 normal, float lifetime = 12f)
    {
        if (bloodPrefab == null) return;

        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;

        float groundY = FindGroundY(point);

        float angle = Mathf.Atan2(normal.x, normal.z) * Mathf.Rad2Deg + 180f;
        var rotation = Quaternion.Euler(0f, angle + 90f, 0f);

        var instance = Object.Instantiate(bloodPrefab, point, rotation);

        var settings = instance.GetComponent<BFX_BloodSettings>();
        if (settings != null)
        {
            settings.AutomaticGroundHeightDetection = false;
            settings.GroundHeight = groundY;
        }

        Object.Destroy(instance, lifetime);
    }

    static float FindGroundY(Vector3 fromPoint)
    {
        Vector3 origin = fromPoint + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return fromPoint.y;
    }
}
