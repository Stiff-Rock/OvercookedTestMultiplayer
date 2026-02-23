using Unity.Netcode;
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