using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }
    [field: SerializeField] public RecipeScriptableObject[] Recipes { get; private set; }

    public Dictionary<DishType, RecipeScriptableObject> RecipesDict { get; private set; }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        RecipesDict = new();
        foreach (var recipe in Recipes)
        {
            RecipesDict[recipe.dishType] = recipe;
        }
    }

    private void Start()
    {
        CreateOrder();
    }

    private void CreateOrder()
    {
        int randomIndex = Random.Range(0, Recipes.Length);
        RecipeScriptableObject selectedRecipe = Recipes[randomIndex];

    }

    public void ServeOrder()
    {

    }
}
