using UnityEngine;

public class DeliveryPoint : InteractiveAppliance
{
    private KitchenOrdersManager ordersManager;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ordersManager = GameObject
            .FindGameObjectWithTag("KitchenOrdersManager")
            .GetComponent<KitchenOrdersManager>();

        if (!ordersManager)
            Debug.LogError("Cannot find 'KitchenOrdersManager' GameObject");
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
