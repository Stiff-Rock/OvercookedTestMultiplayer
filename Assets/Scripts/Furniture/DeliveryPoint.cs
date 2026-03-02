using Unity.Netcode;
using UnityEngine;

public class DeliveryPoint : InteractiveAppliance
{
    private KitchenOrdersManager ordersManager;

    protected override void Start()
    {
        base.Start();

        ordersManager = GameObject
            .FindWithTag("KitchenOrdersManager")
            .GetComponent<KitchenOrdersManager>();
    }

    public void DeliverOrder(DishType type, IngredientData[] baseIngs, IngredientData[] extraIngs)
    {
        Recipe recipe = new(type, baseIngs, extraIngs);
        ordersManager.ServeDish(recipe);
    }

    public override bool HasItem()
    {
        return true;
    }
}
