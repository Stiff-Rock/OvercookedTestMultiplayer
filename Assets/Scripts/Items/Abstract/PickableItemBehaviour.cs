using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PickableItemBehaviour : NetworkBehaviour
{
    private Collider triggerCollider;
    private Collider physicsCollider;
    private Rigidbody rb;

    private NetworkTransform pendingParent;
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
        ToggleColliders(!IsPlaced());
    }

    public override void OnNetworkSpawn()
    {
        if (pendingParent != null)
        {
            transform.SetParent(pendingParent.transform, pendingWorldPositionStays);
            SetParent_ClientRpc(pendingParent.NetworkObjectId, pendingWorldPositionStays);
            pendingParent = null;
        }
    }

    public void NetworkSetParent(Transform newParent, bool worlPositionStays = true)
    {
        NetworkTransform newParentNetTransform = newParent.gameObject.GetComponent<NetworkTransform>();

        if (IsSpawned)
        {
            transform.SetParent(newParent, worlPositionStays);
            SetParent_ClientRpc(newParentNetTransform.NetworkObjectId, worlPositionStays);
        }
        else
        {
            pendingParent = newParentNetTransform;
            pendingWorldPositionStays = worlPositionStays;
        }
    }

    [ClientRpc]
    private void SetParent_ClientRpc(ulong newParentNetId, bool worlPositionStays)
    {
        NetworkObject newParentObj = NetHelpers.GetNetObject(newParentNetId);
        transform.SetParent(newParentObj.transform, worlPositionStays);
    }

    public void ToggleColliders(bool isEnabled)
    {
        if (triggerCollider) triggerCollider.enabled = isEnabled;
        if (physicsCollider) physicsCollider.enabled = isEnabled;

        if (isEnabled)
            rb.constraints = RigidbodyConstraints.None;
        else
            rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void UpdateTransform()
    {
        transform.position = transform.parent.position;
        transform.localRotation = Quaternion.identity;
    }

    public void OnTransformParentChanged()
    {
        bool isPlaced = IsPlaced();
        ToggleColliders(!isPlaced);

        if (isPlaced && currentParent != transform.parent)
            UpdateTransform();

        currentParent = transform.parent;
    }

    #region Helper Methods

    private bool IsPlaced()
    {
        return transform.parent && transform.parent.gameObject.CompareTag("PlaceArea");
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