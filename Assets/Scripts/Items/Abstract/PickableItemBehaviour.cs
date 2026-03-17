using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
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
    [SerializeField] private NetworkTransform networkTransform;

    private NetworkObject pendingParent;
    private Vector3 pendingPos;
    private bool pendingWorldPositionStays;

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
            pendingPos = pendingParent.transform.position;
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

        if (newPositionTransform)
        {
            pendingPos = newPositionTransform.position;
            Debug.Log("newPositionTransform: " + newPositionTransform);
            UpdateTargetPos_ClientRpc(pendingPos);
        }

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

    [ClientRpc]
    private void UpdateTargetPos_ClientRpc(Vector3 pendingPos)
    {
        this.pendingPos = pendingPos;
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);

        bool isPlaced = IsPlaced();
        ToggleColliders(!isPlaced);

        if (isPlaced)
        {
            transform.position = pendingPos;
            transform.localRotation = Quaternion.identity;


            Debug.Log($"parentNetworkObject: {parentNetworkObject}\n" +
                          $"pendingPos: {pendingPos}\n" +
                          $"transform.position: {transform.position}\n" +
                          $"transform.localPosition: {transform.localPosition}\n" +
                          $"PARENT: {transform.parent?.name ?? "No Parent"}");
        }
    }

    public void ToggleColliders(bool isEnabled)
    {
        if (triggerCollider) triggerCollider.enabled = isEnabled;
        if (physicsCollider) physicsCollider.enabled = isEnabled;
        if (networkTransform) networkTransform.enabled = isEnabled;

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