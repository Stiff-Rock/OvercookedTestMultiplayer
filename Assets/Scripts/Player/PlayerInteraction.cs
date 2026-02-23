using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    // References
    [Header("REFERENCES")]
    [SerializeField] private GameObject hand;
    private PlayerController playerController;

    // Controls
    [Header("CONTROLS")]
    [SerializeField] private Key interactKey;
    [SerializeField] private Key pickDropKey;

    // Values
    [Header("INTERACTION PHYSICS")]
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private Vector2 interactionBox;
    private Vector3 halfExtents;
    [SerializeField] private float interactionRange;
    [SerializeField] private float heightOffset;
    private Vector3 yOffset;

    // State
    [Header("STATE (readonly)")]
    [SerializeField] private InteractiveAppliance nearbyAppliance;
    [SerializeField] private PickableItemBehaviour nearbyItem;

    [SerializeField] private PickableItemBehaviour pickedItem;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        yOffset = Vector3.up * heightOffset;
        halfExtents = interactionBox / 2;
    }

    private void Update()
    {
        InteractionCast();
        CheckInputs();
    }

    private void InteractionCast()
    {
        nearbyAppliance = null;
        nearbyItem = null;

        Vector3 center = transform.position + yOffset;

        bool hit = Physics.BoxCast(
            center,
            halfExtents,
            transform.forward,
            out RaycastHit hitInfo,
            transform.rotation,
            interactionRange,
            interactionLayer
        );

        if (hit && hitInfo.collider.attachedRigidbody != null)
        {
            GameObject hitObject = hitInfo.collider.attachedRigidbody.gameObject;

            // Check if its an appliance
            if (hitObject.TryGetComponent(out InteractiveAppliance appliance))
            {
                nearbyAppliance = appliance;
            }
            // Check if its a pickable item
            else if (hitObject.TryGetComponent(out PickableItemBehaviour item))
            {
                nearbyItem = item;
            }
        }
    }

    private void CheckInputs()
    {
        // Check Interact
        if (Keyboard.current[interactKey].wasPressedThisFrame && nearbyAppliance)
            if (pickedItem && nearbyAppliance.HasItem())
                TryMerge();
            else if (!pickedItem)
                nearbyAppliance.OnInteract(playerController);

        // Check Pick/Drop
        if (Keyboard.current[pickDropKey].wasPressedThisFrame)
            PickOrDrop();
    }

    private void TryMerge()
    {
        PickableItemBehaviour held = pickedItem;
        PickableItemBehaviour placed = nearbyAppliance.PlacedItem;

        UtensilBehaviour utensil;
        IngredientBehaviour ingredient;
        bool isIngredientOnAppliance;
        // Utensil is held by player
        if (held is UtensilBehaviour u1 && placed is IngredientBehaviour i1)
        {
            utensil = u1;
            ingredient = i1;
            isIngredientOnAppliance = true;
        }
        // Utensil is on appliance 
        else if (held is IngredientBehaviour i2 && placed is UtensilBehaviour u2)
        {
            utensil = u2;
            ingredient = i2;
            isIngredientOnAppliance = false;
        }
        // Both are utensils
        else if (held is UtensilBehaviour uHeld && placed is UtensilBehaviour uPlaced)
        {
            TryMoveIngredientBetweenUtensils(uHeld, uPlaced);
            return;
        }
        else return;

        // Both are utensils, at least one is plate
        if (!utensil.TryAddIngredient(ingredient)) return;

        if (isIngredientOnAppliance)
        {
            nearbyAppliance.TakeItem();
        }
        else
        {
            DropItem();
        }
    }

    private void PickOrDrop()
    {
        // Deliver dish to delivery point
        if (CanDeliverDish() && NearbyApplianceIsDeilveryPoint(out DeliveryPoint deliveryPoint))
        {
            deliveryPoint.ServeOrder(((UtensilBehaviour)pickedItem).CurrentRecipe);
            ((UtensilBehaviour)pickedItem).EmptyUtensil();
        }
        // Throw to trashcan
        if (CanThrowToTrash())
        {
            nearbyAppliance.PlaceItem(pickedItem);
        }
        // Place item on appliance
        else if (CanPlaceItemOntoAppliance())
        {
            nearbyAppliance.PlaceItem(pickedItem);
            DropItem();
        }
        // Take item from appliance
        else if (CanTakeItemFromAppliance())
        {
            pickedItem = nearbyAppliance.TakeItem();
            pickedItem.gameObject.transform.SetParent(hand.transform);
        }
        // Take nearby item
        else if (CanTakeNearbyItem())
        {
            pickedItem = nearbyItem;
            nearbyItem = null;
            pickedItem.gameObject.transform.SetParent(hand.transform);
        }
        // Drop currently held item
        else if (CanDropHeldItem())
        {
            pickedItem.gameObject.transform.SetParent(null);
            DropItem();
        }
    }

    #region Helper Methods

    public PickableItemBehaviour GetPickedItem()
    {
        return pickedItem;
    }

    public PickableItemBehaviour DropItem()
    {
        PickableItemBehaviour droppedItem = pickedItem;
        pickedItem = null;
        return droppedItem;
    }

    private bool CanThrowToTrash()
    {
        return pickedItem && nearbyAppliance && nearbyAppliance.GetComponent<TrashBehaviour>();
    }

    private bool CanPlaceItemOntoAppliance()
    {
        return pickedItem && nearbyAppliance && !nearbyAppliance.HasItem();
    }

    private bool CanTakeItemFromAppliance()
    {
        return nearbyAppliance && !pickedItem && nearbyAppliance.HasItem();
    }

    private bool CanTakeNearbyItem()
    {
        return nearbyItem && !pickedItem;
    }

    private bool CanDropHeldItem()
    {
        return !nearbyAppliance && pickedItem;
    }

    private bool CanDeliverDish()
    {
        return pickedItem
            && pickedItem is UtensilBehaviour u
            && u.UtensilType == UtensilType.Plate
            && u.CurrentRecipe.GetTotalIngredients() > 0;
    }

    private bool NearbyApplianceIsDeilveryPoint(out DeliveryPoint deliveryPoint)
    {
        deliveryPoint = nearbyAppliance.gameObject.GetComponent<DeliveryPoint>();
        return deliveryPoint;
    }

    private void TryMoveIngredientBetweenUtensils(UtensilBehaviour uHeld, UtensilBehaviour uPlaced)
    {
        UtensilBehaviour plate;
        UtensilBehaviour other;

        if (uHeld.UtensilType == UtensilType.Plate && uPlaced.UtensilType != UtensilType.Plate)
        {
            plate = uHeld;
            other = uPlaced;
        }
        else if (uHeld.UtensilType != UtensilType.Plate && uPlaced.UtensilType == UtensilType.Plate)
        {
            plate = uPlaced;
            other = uHeld;
        }
        else return;

        IngredientBehaviour ingB = other.PeekIngredient();
        if (other.CanTakeIngredient() && plate.TryAddIngredient(ingB))
        {
            other.RemoveIngredient();
        }

        else return;
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 localCenter = new(0, heightOffset, interactionRange / 2);
        Vector3 size = new(interactionBox.x, interactionBox.y, interactionRange);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(localCenter, size);
        Gizmos.matrix = Matrix4x4.identity;
    }

    #endregion
}