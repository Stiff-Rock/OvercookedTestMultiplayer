using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class InteractiveAppliance : NetworkBehaviour
{
    [SerializeField] protected GameObject placeArea;
    protected PlayerController currentPlayer;

    [Header("Readonly Values (Do not assing in editor)")]
    [SerializeField] private PickableItemBehaviour _placedItem;
    public PickableItemBehaviour PlacedItem
    {
        get { return _placedItem; }
        protected set
        {
            _placedItem = value;

            if (_placedItem)
            {
                if (_placedItem.IsIngredient())
                {
                    placedIngredient = _placedItem.GetComponent<IngredientBehaviour>();
                    placedUtensil = null;
                }
                else if (_placedItem.IsUtensil())
                {
                    placedUtensil = _placedItem.GetComponent<UtensilBehaviour>();
                    placedIngredient = null;
                }
            }
            else
            {
                placedIngredient = null;
                placedUtensil = null;
            }

            OnPlacedItemChanged();
        }
    }

    [SerializeField] protected IngredientBehaviour placedIngredient;
    [SerializeField] protected UtensilBehaviour placedUtensil;

    protected virtual void Start()
    {
        if (!_placedItem)
            PlacedItem = GetComponentInChildren<PickableItemBehaviour>();
        else
            PlacedItem = _placedItem;
    }

    public virtual PickableItemBehaviour TakeItem()
    {
        PickableItemBehaviour pickedItem = PlacedItem;
        PlacedItem = null;
        TakeItem_ClientRpc();
        return pickedItem;
    }

    [ClientRpc]
    private void TakeItem_ClientRpc()
    {
        PlacedItem = null;
    }

    public virtual void PlaceItem(PickableItemBehaviour newItem)
    {
        // SERVER guarda referencia
        PlacedItem = newItem;

        // SOLO EL SERVER cambia el parent
        PlacedItem.NetworkSetParent(placeArea.transform);

        PlaceItem_ClientRpc(newItem.NetworkObjectId);
    }

    [ClientRpc]
    private void PlaceItem_ClientRpc(ulong newItemNetId)
    {
        PickableItemBehaviour newItem = NetHelpers.GetNetComponent<PickableItemBehaviour>(newItemNetId);

        // SOLO actualizar referencia
        PlacedItem = newItem;
    }

    public virtual void OnInteract(PlayerController playerController)
    {
        currentPlayer = playerController;
    }

    public virtual void OnPlacedItemChanged()
    {
    }

    public virtual bool HasItem()
    {
        return PlacedItem != null;
    }
}