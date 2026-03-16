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
            return;

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
            pendingWorldPositionStays = worlPositionStays;
        }
    }

    private void UpdateTransform(Transform newPositionTransform)
    {
        bool isPlaced = IsPlaced();

        Transform newParent = transform.parent;
        //Debug.Log($"UpdateTransform: {newPositionTransform.position} || parent: {transform.parent.name}");
        if (isPlaced && currentParent != newParent)
        {
            transform.position = newPositionTransform.position;
            transform.localRotation = Quaternion.identity;
        }

        currentParent = newParent;

        Vector3 newPos = newPositionTransform ? newPositionTransform.position : transform.position;
        //Debug.Log($"SENDING newPos: {newPos}");
        UpdateTransform_ClientRpc(isPlaced, newPos);
    }

    [ClientRpc]
    private void UpdateTransform_ClientRpc(bool isPlaced, Vector3 newPos)
    {
        //Debug.Log($"UpdateTransform_ClientRpc: {newPos} || parent: {transform.parent.name}");
        Transform newParent = transform.parent;

        if (isPlaced && currentParent != newParent)
        {
            transform.position = newPos;
            transform.localRotation = Quaternion.identity;
        }

        currentParent = newParent;

        ToggleColliders_ClientRpc(!isPlaced);
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