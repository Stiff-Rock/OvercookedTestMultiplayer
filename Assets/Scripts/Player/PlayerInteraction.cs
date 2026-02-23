using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
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

    [Header("STATE (readonly)")]
    // Synched
    public NetworkVariable<NetworkObjectReference> pickedItemNet = new();
    [SerializeField] private PickableItemBehaviour ownPickedItem;

    // Local
    [SerializeField] private PickableItemBehaviour ownNearbyItem;
    [SerializeField] private InteractiveAppliance ownNearbyAppliance;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        playerController = GetComponent<PlayerController>();
        yOffset = Vector3.up * heightOffset;
        halfExtents = interactionBox / 2;
    }

    private void Update()
    {
        if (!IsOwner) return;

        InteractionCast();
        CheckInputs();
    }

    private void InteractionCast()
    {
        ownNearbyAppliance = null;
        ownNearbyItem = null;

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
                ownNearbyAppliance = appliance;
            }
            // Check if its a pickable item
            else if (hitObject.TryGetComponent(out PickableItemBehaviour item))
            {
                ownNearbyItem = item;
            }
        }
    }

    private void CheckInputs()
    {
        // Check Interact
        if (Keyboard.current[interactKey].wasPressedThisFrame && ownNearbyAppliance)
            if (ownPickedItem && ownNearbyAppliance.HasItem())
            {
                TryMerge_ServerRpc(ownPickedItem.NetworkObjectId, ownNearbyAppliance.NetworkObjectId);
            }
            else if (!ownPickedItem)
                ownNearbyAppliance.OnInteract(playerController);

        // Check Pick/Drop
        if (Keyboard.current[pickDropKey].wasPressedThisFrame)
        {
            PickOrDrop_ServerRpc(
                ownNearbyAppliance.NetworkObjectId,
                ownNearbyItem.NetworkObjectId,
                ownPickedItem.NetworkObjectId
            );
        }
    }

    [ServerRpc]
    private void TryMerge_ServerRpc(ulong pickedItemId, ulong nearbyApplianceId)
    {
        PickableItemBehaviour held = NetHelpers.GetNetComponent<PickableItemBehaviour>(pickedItemId);
        PickableItemBehaviour placed = NetHelpers.GetNetComponent<PickableItemBehaviour>(nearbyApplianceId);

        if (held == null)
        {
            Debug.LogWarning($"Could not find held item with id '{pickedItemId}'");
            return;
        }

        if (placed == null)
        {
            Debug.LogWarning($"Could not find placed item with id '{nearbyApplianceId}'");
            return;
        }

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
            ownNearbyAppliance.TakeItem();
        }
        else
        {
            DropItem();
        }
    }

    [ServerRpc]
    private void PickOrDrop_ServerRpc(ulong nearbyApplianceId, ulong nearbyItemId, ulong pickedItemId)
    {
        InteractiveAppliance nearbyAppliance = NetHelpers.GetNetComponent<InteractiveAppliance>(nearbyApplianceId);

        if (nearbyAppliance == null)
        {
            Debug.LogWarning($"Could not find nearbyAppliance with id '{nearbyApplianceId}'");
            return;
        }

        PickableItemBehaviour nearbyItem = NetHelpers.GetNetComponent<PickableItemBehaviour>(nearbyItemId);

        if (nearbyItem == null)
        {
            Debug.LogWarning($"Could not find nearbyItem with id '{nearbyItemId}'");
            return;
        }

        PickableItemBehaviour pickedItem = NetHelpers.GetNetComponent<PickableItemBehaviour>(pickedItemId);

        if (pickedItem == null)
        {
            Debug.LogWarning($"Could not find pickedItem with id '{pickedItemId}'");
            return;
        }

        // TODO: TRATAR CADA CASO INDIVIDUAL DE PickOrDrop_ServerRpc

        // Deliver dish to delivery point
        if (CanDeliverDish(pickedItem) && NearbyApplianceIsDeilveryPoint(nearbyAppliance, out DeliveryPoint deliveryPoint))
        {
            Recipe currentRecipe = ((UtensilBehaviour)pickedItem).CurrentRecipe;

            deliveryPoint.DeliverOrder_ServerRpc(
                currentRecipe.DishType,
                currentRecipe.GetBaseIngredients().ToArray(),
                currentRecipe.GetExtraIngredients().ToArray()
            );

            ((UtensilBehaviour)pickedItem).EmptyUtensil();
        }
        // Throw to trashcan
        if (CanThrowToTrash(pickedItem, nearbyAppliance))
        {
            nearbyAppliance.PlaceItem(pickedItem);
        }
        // Place item on appliance
        else if (CanPlaceItemOntoAppliance(pickedItem, nearbyAppliance))
        {
            nearbyAppliance.PlaceItem(pickedItem);
            DropItem();
        }
        // Take item from appliance
        else if (CanTakeItemFromAppliance(nearbyAppliance, pickedItem))
        {
            ownPickedItem = nearbyAppliance.TakeItem();
            pickedItem.gameObject.transform.SetParent(hand.transform);
        }
        // Take nearby item
        else if (CanTakeNearbyItem(nearbyItem, pickedItem))
        {
            pickedItem = nearbyItem;
            ownNearbyItem = null;
            pickedItem.gameObject.transform.SetParent(hand.transform);
        }
        // Drop currently held item
        else if (CanDropHeldItem(nearbyAppliance, pickedItem))
        {
            pickedItem.gameObject.transform.SetParent(null);
            DropItem();
        }
    }


    #region Helper Methods

    private PickableItemBehaviour DropItem()
    {
        PickableItemBehaviour droppedItem = ownPickedItem;
        ownPickedItem = null;
        return droppedItem;
    }

    private bool CanThrowToTrash(PickableItemBehaviour pickedItem, InteractiveAppliance nearbyAppliance)
    {
        return pickedItem && nearbyAppliance && nearbyAppliance.GetComponent<TrashBehaviour>();
    }

    private bool CanPlaceItemOntoAppliance(PickableItemBehaviour pickedItem, InteractiveAppliance nearbyAppliance)
    {
        return pickedItem && nearbyAppliance && !nearbyAppliance.HasItem();
    }

    private bool CanTakeItemFromAppliance(InteractiveAppliance nearbyAppliance, PickableItemBehaviour pickedItem)
    {
        return nearbyAppliance && !pickedItem && nearbyAppliance.HasItem();
    }

    private bool CanTakeNearbyItem(PickableItemBehaviour nearbyItem, PickableItemBehaviour pickedItem)
    {
        return nearbyItem && !pickedItem;
    }

    private bool CanDropHeldItem(InteractiveAppliance nearbyAppliance, PickableItemBehaviour pickedItem)
    {
        return !nearbyAppliance && pickedItem;
    }

    private bool CanDeliverDish(PickableItemBehaviour pickedItem)
    {
        return pickedItem
            && pickedItem is UtensilBehaviour u
            && u.UtensilType == UtensilType.Plate
            && u.CurrentRecipe.GetTotalIngredients() > 0;
    }

    private bool NearbyApplianceIsDeilveryPoint(InteractiveAppliance nearbyAppliance, out DeliveryPoint deliveryPoint)
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

        // TODO: SYNCH TryMoveIngredientBetweenUtensils
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