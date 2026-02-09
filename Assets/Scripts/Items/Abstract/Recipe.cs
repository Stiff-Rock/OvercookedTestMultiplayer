using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Recipe
{
    [Header("Type")]
    [SerializeField] private DishType dishType;

    [Header("Ingredients")]
    [SerializeField] private List<IngredientType> baseIngredients;
    [SerializeField] private List<IngredientType> extraIngredients;

    // Cache
    [SerializeField] private List<DishType> possibleRecipes;
    private RecipeScriptableObject matchedRecipe;

    public Recipe()
    {
        dishType = DishType.None;
        baseIngredients = new();
        extraIngredients = new();
    }

    public bool TryAddIngredient(IngredientType newIngredient)
    {
        if (AlreadyContainsIngredient(newIngredient)) return false;

        // If the recipe is already matched and finished, check for the extra ingredients
        if (RecipeIsFinished())
        {
            bool compatibleExtra = matchedRecipe.extraIngredients.Contains(newIngredient);

            if (compatibleExtra)
                extraIngredients.Add(newIngredient);

            return compatibleExtra;
        }

        possibleRecipes ??= new(GameController.Instance.RecipesDict.Keys);

        // Check if the new ingredient is present in any of the possible recipes
        bool ingredientAccepted = possibleRecipes.Any(dish =>
        {
            RecipeScriptableObject recipe = GameController.Instance.RecipesDict[dish];
            return recipe.requiredIngredients.Contains(newIngredient);
        });

        if (ingredientAccepted)
        {
            baseIngredients.Add(newIngredient);
            FilterPossibleRecipes();
        }

        return ingredientAccepted;
    }

    public bool Matches(Recipe recipe)
    {
        return recipe.dishType == dishType
            && baseIngredients.All(i => recipe.baseIngredients.Contains(i))
            && extraIngredients.All(i => recipe.extraIngredients.Contains(i));
    }

    #region Helper Methods
    private bool AlreadyContainsIngredient(IngredientType newIngredient)
    {
        return baseIngredients.Contains(newIngredient) || extraIngredients.Contains(newIngredient);
    }

    // Filter recipes based on the current base ingredients
    private void FilterPossibleRecipes()
    {
        possibleRecipes.RemoveAll(dish => !IsCompatibleDish(dish));

        if (possibleRecipes.Count == 1)
        {
            dishType = possibleRecipes.First();
            matchedRecipe = GameController.Instance.RecipesDict[dishType];
        }
    }

    // Helper method to check a dish is compatible with the current base ingredients
    private bool IsCompatibleDish(DishType dish)
    {
        RecipeScriptableObject recipe = GameController.Instance.RecipesDict[dish];

        if (recipe == null)
        {
            Debug.LogError($"Recipe for dish {dish} not found in the dictionary.");
            return false;
        }

        return baseIngredients.All(i => recipe.requiredIngredients.Contains(i));
    }

    private bool RecipeIsFinished()
    {
        return matchedRecipe && matchedRecipe.requiredIngredients.Count() == baseIngredients.Count;
    }

    #endregion
}
