using UnityEngine;

public class IngredientCrate : InteractiveAppliance
{
    private Animation anim;

    [SerializeField] private PickableItemBehaviour ingredient;

    private void Awake()
    {
        anim = GetComponentInChildren<Animation>();
    }

    public override PickableItemBehaviour TakeItem()
    {
        anim.Play();
        PickableItemBehaviour pib = Instantiate(ingredient.gameObject).GetComponent<PickableItemBehaviour>();
        pib.NetworkObject.Spawn();
        return pib;
    }

    public override bool HasItem()
    {
        return true;
    }
}
