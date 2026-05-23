using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    private static bool alreadyExists = false;

    private void Awake()
    {
        if (!alreadyExists)
        {
            alreadyExists = true;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}