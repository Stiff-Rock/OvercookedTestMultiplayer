using UnityEngine;
using Unity.Netcode;

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
        PlayAnimationClientRpc();

        PickableItemBehaviour pib = Instantiate(ingredient.gameObject).GetComponent<PickableItemBehaviour>();
        pib.NetworkObject.Spawn();
        return pib;
    }

    [ClientRpc]
    private void PlayAnimationClientRpc()
    {
        anim.Play();
    }

    public override bool HasItem()
    {
        return true;
    }
}