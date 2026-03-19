using UnityEngine;

public class NetworkManagerInstance : MonoBehaviour
{
    private static NetworkManagerInstance Instance;

    private void Awake()
    {
        if (Instance)
            Destroy(gameObject);
        else
            Instance = this;
    }
}
