using UnityEngine;

// TODO: Dibujo/etiqueta de que ingrediente da el IngredientCrate
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
        return Instantiate(ingredient.gameObject).GetComponent<PickableItemBehaviour>();
    }

    public override bool HasItem()
    {
        return true;
    }
}
