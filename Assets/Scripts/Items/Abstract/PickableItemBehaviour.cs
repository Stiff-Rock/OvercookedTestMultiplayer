using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PickableItemBehaviour : NetworkBehaviour
{
    private Collider triggerCollider;
    private Collider physicsCollider;
    private Rigidbody rb;

    private NetworkObject pendingParent;
    private Vector3 pendingPosition;
    private bool pendingWorldPositionStays;

    private Transform currentParent;

    // TODO: CHANGE TO OnNetworkSpawn
    protected virtual void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        physicsCollider = transform.GetChild(0).gameObject.GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        ToggleColliders_ClientRpc(!IsPlaced());
    }

    public override void OnNetworkSpawn()
    {
        if (pendingParent != null)
        {
            NetworkObject.TrySetParent(pendingParent.transform, pendingWorldPositionStays);
            UpdateTransform(pendingParent.transform);
            pendingParent = null;
        }
    }

    public void NetworkSetParent(Transform newPositionTransform, bool worlPositionStays = true)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Parenting can only be initiated by the Server. \n" + System.Environment.StackTrace);
            return;
        }

        NetworkObject newParentNetTransform = newPositionTransform ?
            newPositionTransform.parent.gameObject.GetComponent<NetworkObject>()
            : null;

        if (IsSpawned)
        {
            NetworkObject.TrySetParent(newParentNetTransform, worlPositionStays);
            UpdateTransform(newPositionTransform);
        }
        else
        {
            pendingParent = newParentNetTransform;
            pendingPosition = newPositionTransform ? newPositionTransform.position : transform.position;
            pendingWorldPositionStays = worlPositionStays;
        }
    }

    [ClientRpc]
    public void ToggleColliders_ClientRpc(bool isEnabled)
    {
        if (triggerCollider) triggerCollider.enabled = isEnabled;
        if (physicsCollider) physicsCollider.enabled = isEnabled;

        if (isEnabled)
            rb.constraints = RigidbodyConstraints.None;
        else
            rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void UpdateTransform(Transform newPositionTransform)
    {
        bool isPlaced = IsPlaced();

        Transform newParent = transform.parent;

        if (isPlaced && currentParent != newParent)
        {
            transform.position = newPositionTransform.position;
            transform.localRotation = Quaternion.identity;
        }

        currentParent = newParent;

        ToggleColliders_ClientRpc(!isPlaced);
    }

    #region Helper Methods

    private bool IsPlaced()
    {
        if (transform.parent == null) return false;

        Transform isInPlaceArea = transform.parent.Cast<Transform>()
            .FirstOrDefault(c => c.CompareTag("PlaceArea"));

        return isInPlaceArea;
    }

    public bool IsIngredient()
    {
        return this is IngredientBehaviour;
    }

    public bool IsUtensil()
    {
        return this is UtensilBehaviour;
    }

    #endregion
}