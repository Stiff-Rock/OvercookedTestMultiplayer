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
    private NetworkVariable<Vector3> pendingPos = new();
    private bool pendingWorldPositionStays;

    private Transform currentParent;

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
            NetworkObject.TrySetParent(pendingParent.transform, pendingWorldPositionStays);
            pendingPos.Value = pendingParent.transform.position;
            pendingParent = null;
        }
    }

    public void NetworkSetParent(Transform newPositionTransform, bool worlPositionStays = true)
    {
        if (!IsServer)
            return;

        if (newPositionTransform)
            pendingPos.Value = newPositionTransform.position;

        NetworkObject newParentNetTransform = newPositionTransform ?
            newPositionTransform.parent.gameObject.GetComponent<NetworkObject>()
            : null;

        if (IsSpawned)
        {
            NetworkObject.TrySetParent(newParentNetTransform, worlPositionStays);
        }
        else
        {
            pendingParent = newParentNetTransform;
            pendingWorldPositionStays = worlPositionStays;
        }
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);

        bool isPlaced = IsPlaced();

        Transform newParent = transform.parent;
        if (isPlaced && currentParent != newParent)
        {
            transform.position = pendingPos.Value;
            transform.localRotation = Quaternion.identity;
            Debug.Log($"transform.position: {transform.position}");
            Debug.Log($"transform.localRotation: {transform.position}");
            Debug.Log($"transform.position: {transform.position}");
        }

        currentParent = newParent;

        ToggleColliders(!isPlaced);
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