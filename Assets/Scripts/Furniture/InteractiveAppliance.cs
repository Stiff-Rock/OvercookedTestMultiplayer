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
            if (_placedItem == value) return;

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

    protected IngredientBehaviour placedIngredient;
    protected UtensilBehaviour placedUtensil;

    // TODO: CHANGE TO OnNetworkSpawn
    protected virtual void Start()
    {
        PlacedItem = placeArea.GetComponentInChildren<PickableItemBehaviour>();
    }

    public virtual PickableItemBehaviour TakeItem()
    {
        PickableItemBehaviour pickedItem = PlacedItem;
        PlacedItem = null;
        return pickedItem;
    }

    public virtual void PlaceItem(PickableItemBehaviour newItem)
    {
        // Store item
        PlacedItem = newItem;

        // Make it a child and put it in the place position
        PlacedItem.gameObject.transform.SetParent(placeArea.transform);
    }

    // Virtual method to be overridden by child classes
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