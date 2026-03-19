using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PickableItemBehaviour : NetworkBehaviour
{
    [Header("Base References")]
    private Collider triggerCollider;
    private Collider physicsCollider;
    private Rigidbody rb;

    private Transform pendingParentTransform;
    private bool pendingWorldPositionStays;

    protected virtual void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        physicsCollider = transform.GetChild(0).gameObject.GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        ToggleColliders(!IsPlaced(out var _));
    }

    public override void OnNetworkSpawn()
    {
        if (pendingParentTransform != null)
        {
            UpdateTransform(pendingParentTransform, pendingWorldPositionStays);
            pendingParentTransform = null;
        }
    }

    public void NetworkSetParent(Transform newTransform, bool worlPositionStays = true)
    {
        if (!IsServer) return;

        if (IsSpawned)
        {
            UpdateTransform(newTransform, worlPositionStays);
        }
        else
        {
            pendingParentTransform = newTransform;
            pendingWorldPositionStays = worlPositionStays;
        }
    }

    private void UpdateTransform(Transform newTransformSocket, bool worlPositionStays)
    {
        Transform newParent = newTransformSocket ? newTransformSocket.parent : null;
        NetworkObject.TrySetParent(newParent, worlPositionStays);
    }


    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        bool isPlaced = IsPlaced(out Transform placeArea);
        ToggleColliders(!isPlaced);

        if (isPlaced)
        {

            transform.SetPositionAndRotation(placeArea.position, Quaternion.identity);
        }
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

    private bool IsPlaced(out Transform placeAreaTransform)
    {
        placeAreaTransform = null;

        if (transform.parent == null) return false;

        placeAreaTransform = transform.parent.Cast<Transform>()
            .FirstOrDefault(t =>
            {
                bool isPlaceArea = t.CompareTag("PlaceArea");
                return isPlaceArea;
            });

        return placeAreaTransform;
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