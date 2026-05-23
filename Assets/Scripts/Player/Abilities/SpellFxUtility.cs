using UnityEngine;

public static class SpellFxUtility
{
    public static void SetPlaybackSpeed(GameObject go, float speed)
    {
        if (go == null || Mathf.Approximately(speed, 1f)) return;

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.simulationSpeed = speed;
        }

        foreach (var anim in go.GetComponentsInChildren<Animator>(true))
            anim.speed = speed;

        foreach (var anim in go.GetComponentsInChildren<Animation>(true))
        {
            foreach (AnimationState state in anim)
                state.speed = speed;
        }
    }

    public static void DisableObstructingComponents(GameObject go)
    {
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }
}
