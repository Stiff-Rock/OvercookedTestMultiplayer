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
                //TODO:
                TryMerge_ServerRpc(NetworkObjectId);
            else if (!ownPickedItem)
                //TODO:
                ownNearbyAppliance.OnInteract(playerController);

        // Check Pick/Drop
        if (Keyboard.current[pickDropKey].wasPressedThisFrame)
            PickOrDrop_ServerRpc(NetworkObjectId);
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
    private void PickOrDrop_ServerRpc(ulong playerNetId)
    {
        PlayerInteraction pI = NetHelpers.GetNetComponent<PlayerInteraction>(playerNetId);

        // Deliver dish to delivery point
        if (CanDeliverDish(pI.ownPickedItem) && NearbyApplianceIsDeilveryPoint(pI.ownNearbyAppliance, out DeliveryPoint deliveryPoint))
        {
            Recipe currentRecipe = ((UtensilBehaviour)pI.ownPickedItem).CurrentRecipe;

            deliveryPoint.DeliverOrder(
                currentRecipe.DishType,
                currentRecipe.GetBaseIngredients().ToArray(),
                currentRecipe.GetExtraIngredients().ToArray()
            );

            ((UtensilBehaviour)pI.ownPickedItem).EmptyUtensil();
        }
        // Throw to trashcan
        if (CanThrowToTrash(pI.ownPickedItem, pI.ownNearbyAppliance))
        {
            pI.ownNearbyAppliance.PlaceItem(pI.ownPickedItem);
        }
        // Place item on appliance
        else if (CanPlaceItemOntoAppliance(pI.ownPickedItem, pI.ownNearbyAppliance))
        {
            pI.ownNearbyAppliance.PlaceItem(pI.ownPickedItem);
            DropItem(pI);
        }
        // Take item from appliance
        else if (CanTakeItemFromAppliance(pI.ownNearbyAppliance, pI.ownPickedItem))
        {
            pI.ownPickedItem = pI.ownNearbyAppliance.TakeItem();
            pI.ownPickedItem.NetworkSetParent(pI.hand.transform);
        }
        // Take nearby item
        else if (CanTakeNearbyItem(pI.ownNearbyItem, pI.ownPickedItem))
        {
            pI.ownPickedItem = pI.ownNearbyItem;
            pI.ownNearbyItem = null;
            pI.ownPickedItem.NetworkSetParent(pI.hand.transform);
        }
        // Drop currently held item
        else if (CanDropHeldItem(pI.ownNearbyAppliance, pI.ownPickedItem))
        {
            pI.ownPickedItem.NetworkSetParent(null);
            DropItem(pI);
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