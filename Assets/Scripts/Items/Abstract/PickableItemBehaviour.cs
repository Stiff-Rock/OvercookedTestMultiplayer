using System.Linq;
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

        NetworkObject newParentNetTransform = newPositionTransform.parent.gameObject.GetComponent<NetworkObject>();

        if (IsSpawned)
        {
            NetworkObject.TrySetParent(newParentNetTransform, worlPositionStays);
            NetworkObject.transform.position = newPositionTransform.position;
        }
        else
        {
            pendingParent = newParentNetTransform;
            pendingWorldPositionStays = worlPositionStays;
        }
    }

    [ClientRpc]
    public void ToggleColliders_ClientRpc(bool isEnabled)
    {
        Debug.Log($"isEnabled: " + isEnabled);

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
        ToggleColliders_ClientRpc(!isPlaced);

        if (isPlaced && currentParent != transform.parent)
            UpdateTransform();

        currentParent = transform.parent;
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