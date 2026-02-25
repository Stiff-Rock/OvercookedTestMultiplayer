using System.Collections.Generic;
using UnityEngine;

public class RecipesManager : MonoBehaviour
{
    public static RecipesManager Instance { get; private set; }
    
    // Dish recipes
    [field: SerializeField] public RecipeScriptableObject[] Recipes { get; private set; }
    public Dictionary<DishType, RecipeScriptableObject> DishToRecipe { get; private set; }

    // Pan recipes
    [field: SerializeField] public IngredientData[] PanAcceptedIngredients { get; private set; }

    // Pot recipes
    [field: SerializeField] public IngredientData[] PotAcceptedIngredients { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDishDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDishDictionary()
    {
        DishToRecipe = new();
        foreach (RecipeScriptableObject recipe in Recipes)
        {
            DishToRecipe[recipe.DishType] = recipe;
        }
    }
}
