using System.Collections;
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
        ToggleColliders(!IsPlaced());
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
        transform.SetParent(newParent, worlPositionStays);

        ulong networkObjectId;
        Vector3 newPos;
        if (newParent)
        {
            transform.SetPositionAndRotation(newTransformSocket.position, Quaternion.identity);

            newPos = newTransformSocket.position;
            networkObjectId = newParent.GetComponent<NetworkObject>().NetworkObjectId;
        }
        else
        {
            newPos = default;
            networkObjectId = ulong.MaxValue;
        }

        UpdateTransform_ClientRpc(networkObjectId, newPos, worlPositionStays);
    }

    [ClientRpc]
    private void UpdateTransform_ClientRpc(ulong parentTransformId, Vector3 newPos, bool worlPositionStays)
    {
        StopAllCoroutines();
        if (networkTransform) networkTransform.enabled = false;

        Transform newParent = parentTransformId != ulong.MaxValue ?
            NetHelpers.GetNetObject(parentTransformId).transform
            : null;

        transform.SetParent(newParent, worlPositionStays);

        if (newParent)
        {
            transform.SetPositionAndRotation(newPos, Quaternion.identity);
        }

        StartCoroutine(ReenableNetworkTransform());
    }

    private IEnumerator ReenableNetworkTransform()
    {
        yield return new WaitForFixedUpdate();
        if (networkTransform) {
            networkTransform.enabled = true; 
        }
    }

    private void OnTransformParentChanged()
    {
        bool isPlaced = IsPlaced();
        ToggleColliders(!isPlaced);
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