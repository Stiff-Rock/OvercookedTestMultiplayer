using Unity.Burst.Intrinsics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : NetworkBehaviour
{
    // References
    [Header("REFERENCES")]
    public GameObject hand;
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
    public PickableItemBehaviour ownPickedItem;
    public PickableItemBehaviour ownNearbyItem;
    public InteractiveAppliance ownNearbyAppliance;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

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
                TryMerge_ServerRpc(NetworkObjectId);
            else if (!ownPickedItem)
                ownNearbyAppliance.OnInteract(playerController);

        // Check Pick/Drop
        if (Keyboard.current[pickDropKey].wasPressedThisFrame)
        {
            // Pasamos los IDs de objetos cercanos al servidor
            ulong nearbyItemId = ownNearbyItem ? ownNearbyItem.NetworkObjectId : 0;
            ulong nearbyApplianceId = ownNearbyAppliance ? ownNearbyAppliance.NetworkObjectId : 0;

            PickOrDrop_ServerRpc(nearbyItemId, nearbyApplianceId);
        }
    }

    [ServerRpc]
    private void TryMerge_ServerRpc(ulong playerNetId)
    {
        PlayerInteraction pI = NetHelpers.GetNetComponent<PlayerInteraction>(playerNetId);

        PickableItemBehaviour held = pI.ownPickedItem;
        PickableItemBehaviour placed = pI.ownNearbyAppliance.PlacedItem;

        UtensilBehaviour utensil;
        IngredientBehaviour ingredient;
        bool isIngredientOnAppliance;

        if (held is UtensilBehaviour u1 && placed is IngredientBehaviour i1)
        {
            utensil = u1;
            ingredient = i1;
            isIngredientOnAppliance = true;
        }
        else if (held is IngredientBehaviour i2 && placed is UtensilBehaviour u2)
        {
            utensil = u2;
            ingredient = i2;
            isIngredientOnAppliance = false;
        }
        else if (held is UtensilBehaviour uHeld && placed is UtensilBehaviour uPlaced)
        {
            TryMoveIngredientBetweenUtensils(uHeld, uPlaced);
            return;
        }
        else return;

        if (!utensil.TryAddIngredient(ingredient)) return;

        if (isIngredientOnAppliance)
        {
            pI.ownNearbyAppliance.TakeItem();
        }
        else
        {
            DropItem(pI);
        }
    }

    [ServerRpc]
    private void PickOrDrop_ServerRpc(ulong nearbyItemId, ulong nearbyApplianceId)
    {
        PlayerInteraction pI = this;

        PickableItemBehaviour nearbyItem = nearbyItemId != 0 ? NetHelpers.GetNetComponent<PickableItemBehaviour>(nearbyItemId) : null;
        InteractiveAppliance nearbyAppliance = nearbyApplianceId != 0 ? NetHelpers.GetNetComponent<InteractiveAppliance>(nearbyApplianceId) : null;

        PickableItemBehaviour pickedItem = pI.ownPickedItem;

        // Deliver dish to delivery point
        if (CanDeliverDish(pickedItem) && NearbyApplianceIsDeilveryPoint(nearbyAppliance, out DeliveryPoint deliveryPoint))
        {
            Recipe currentRecipe = ((UtensilBehaviour)pickedItem).CurrentRecipe;

            deliveryPoint.DeliverOrder(
                currentRecipe.DishType,
                currentRecipe.GetBaseIngredients().ToArray(),
                currentRecipe.GetExtraIngredients().ToArray()
            );

            ((UtensilBehaviour)pickedItem).EmptyUtensil();
            return;
        }

        // Throw to trashcan
        if (CanThrowToTrash(pickedItem, nearbyAppliance))
        {
            nearbyAppliance.PlaceItem(pickedItem);
            pI.ownPickedItem = null;
            return;
        }

        // Place item on appliance
        if (CanPlaceItemOntoAppliance(pickedItem, nearbyAppliance))
        {
            nearbyAppliance.PlaceItem(pickedItem);
            pI.ownPickedItem = null;
            return;
        }

        // Take item from appliance
        if (CanTakeItemFromAppliance(nearbyAppliance, pickedItem))
        {
            pI.ownPickedItem = nearbyAppliance.TakeItem();
            pI.ownPickedItem.NetworkSetParent(pI.hand.transform);
            return;
        }

        // Take nearby item
        if (CanTakeNearbyItem(nearbyItem, pickedItem))
        {
            pI.ownPickedItem = nearbyItem;
            pI.ownPickedItem.NetworkSetParent(pI.hand.transform);
            return;
        }

        // Drop currently held item
        if (CanDropHeldItem(nearbyAppliance, pickedItem))
        {
            pI.ownPickedItem.NetworkSetParent(null);
            pI.ownPickedItem = null;
            return;
        }
    }

    #region Helper Methods

    private PickableItemBehaviour DropItem(PlayerInteraction pI)
    {
        PickableItemBehaviour droppedItem = pI.ownPickedItem;
        pI.ownPickedItem = null;
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
        deliveryPoint = nearbyAppliance?.gameObject.GetComponent<DeliveryPoint>();
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