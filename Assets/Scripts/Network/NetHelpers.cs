using Unity.Netcode;

public static class NetHelpers
{
    public static NetworkObject GetNetObject(ulong objectId)
    {
        if (objectId == 0) return default;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(
                objectId,
                out NetworkObject netObj
            );

        return netObj;
    }

    public static T GetNetComponent<T>(ulong objectId)
    {
        if (objectId == 0) return default;

        NetworkObject netObj = GetNetObject(objectId);

        if (netObj && netObj.TryGetComponent(out T component))
            return component;


        return default;
    }
}
