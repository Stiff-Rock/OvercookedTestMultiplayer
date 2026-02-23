using Unity.Netcode;

public static class NetHelpers
{
    public static T GetNetComponent<T>(ulong objectId)
    {
        if (objectId == 0) return default;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out T component))
            {
                return component;
            }
        }

        return default;
    }
}
