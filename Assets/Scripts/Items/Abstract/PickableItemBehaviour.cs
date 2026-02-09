using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PickableItemBehaviour : MonoBehaviour
{
    private Collider triggerCollider;
    private Collider physicsCollider;
    private Rigidbody rb;

    protected virtual void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        physicsCollider = transform.GetChild(0).gameObject.GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        ToggleColliders(transform.parent == null);
    }

    public void ToggleColliders(bool isEnabled)
    {
        if (triggerCollider) triggerCollider.enabled = isEnabled;
        if (physicsCollider) physicsCollider.enabled = isEnabled;

        if (isEnabled)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
        else
        {
            rb.constraints =
                // Freeze Position
                RigidbodyConstraints.FreezePositionX
                | RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezePositionZ
                // Freeze Rotation
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationY
                | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void UpdateTransform()
    {
        transform.position = transform.parent.position;
        transform.localRotation = Quaternion.identity;
    }

    public void OnTransformParentChanged()
    {
        bool newParentTransformExists = transform.parent != null;
        ToggleColliders(!newParentTransformExists);
        if (newParentTransformExists) UpdateTransform();
    }

    #region Helper Methods

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
