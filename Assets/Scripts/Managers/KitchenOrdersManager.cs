using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkObject))]
public class KitchenOrdersManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orderRowTransform;
    [SerializeField] private GameObject kitchenOrderPanelPrefab;
    [field: SerializeField] public IngredientVisuals IngredientVisualsSO { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxOrders = 5;
    [SerializeField] private int maxIngredientsLimit = 4;
    [SerializeField] private float orderLifespan = 180.0f;

    [Header("Game State")]
    [SerializeField] private List<KitchenOrder> kitchenOrders;
    private List<GameObject> kitchenOrderPanels;

    private void Awake()
    {
        kitchenOrders = new();
        kitchenOrderPanels = new();
    }

    public void CreateOrder()
    {
        if (kitchenOrders.Count >= maxOrders) return;

        // Select a random recipe
        int randomIndex = Random.Range(0, RecipesManager.Instance.Recipes.Length);
        RecipeScriptableObject selectedRecipe = RecipesManager.Instance.Recipes[randomIndex];

        // Extract recipe data
        DishType type = selectedRecipe.DishType;
        IngredientData[] baseIngredients = selectedRecipe.RequiredIngredients;
        List<IngredientData> possibleExtraIngredients = new(selectedRecipe.ExtraIngredients);

        // Select a random extra ingredients amount
        int limit = Mathf.Min(possibleExtraIngredients.Count, maxIngredientsLimit - baseIngredients.Length);
        int extrasAmount = Random.Range(0, limit + 1);

        Recipe newOrderRecipe = new(type, baseIngredients);
        for (int i = 0; i < extrasAmount; i++)
        {
            if (possibleExtraIngredients.Count <= 0) break;

            int newExtraIndex = Random.Range(0, possibleExtraIngredients.Count);
            IngredientData newExtra = possibleExtraIngredients[newExtraIndex];

            if (!newOrderRecipe.TryAddExtra(newExtra))
                i--;

            possibleExtraIngredients.RemoveAt(newExtraIndex);
        }

        // Create the KitchenOrder
        KitchenOrder serverOrder = new GameObject($"KitchenOrder-{newOrderRecipe}").AddComponent<KitchenOrder>();

        serverOrder.OnExpire.AddListener(ScoreManager.Instance.PenalizeScore);
        serverOrder.OnExpire.AddListener(() => RemoveOrder(serverOrder));

        serverOrder.Initialize(newOrderRecipe, orderLifespan, IngredientVisualsSO);
        kitchenOrders.Add(serverOrder);

        CreateyOrder_ClientRpc(
            newOrderRecipe.DishType,
            newOrderRecipe.GetBaseIngredients().ToArray(),
            newOrderRecipe.GetExtraIngredients().ToArray()
        );
    }

    [ClientRpc]
    private void CreateyOrder_ClientRpc(DishType dishType, IngredientData[] baseTypes, IngredientData[] extraTypes)
    {
        Recipe newOrderRecipe = new(dishType, baseTypes, extraTypes);
        GameObject newOrderPanel = Instantiate(kitchenOrderPanelPrefab, orderRowTransform);

        KitchenOrder uiOrder = newOrderPanel.GetComponent<KitchenOrder>();

        uiOrder.Initialize(newOrderRecipe, orderLifespan, IngredientVisualsSO);

        kitchenOrderPanels.Add(newOrderPanel);
    }

    public void ServeDish(Recipe recipe)
    {
        if (!IsServer) return;

        foreach (KitchenOrder order in kitchenOrders)
        {
            // Check if the served dish matches any placed orders
            if (order.Recipe.Matches(recipe))
            {
                ScoreManager.Instance.RewardScore(order);
                RemoveOrder(order);
                return;
            }
        }

        // No order matched the served dish
        ScoreManager.Instance.PenalizeScore();
    }

    private void RemoveOrder(KitchenOrder orderToDelete)
    {
        if (!IsServer) return;

        int orderToDeleteIndex = kitchenOrders.IndexOf(orderToDelete);
        kitchenOrders.RemoveAt(orderToDeleteIndex);

        RemoveOrderPanel_ClientRpc(orderToDeleteIndex);
    }

    [ClientRpc]
    private void RemoveOrderPanel_ClientRpc(int index)
    {
        if (index >= 0 && index < kitchenOrderPanels.Count)
        {
            GameObject panel = kitchenOrderPanels[index];
            kitchenOrderPanels.RemoveAt(index);
            Destroy(panel);
        }
    }
}
