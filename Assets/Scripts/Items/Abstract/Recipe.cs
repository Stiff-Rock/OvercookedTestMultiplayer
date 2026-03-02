using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Recipe
{
    [Header("Type")]
    [SerializeField] public DishType DishType { get; private set; }

    [Header("Ingredients")]
    [SerializeField] private List<IngredientData> baseIngredients;
    [SerializeField] private List<IngredientData> extraIngredients;

    // Cache
    [SerializeField] private List<DishType> possibleRecipes;
    private RecipeScriptableObject matchedRecipe;

    public Recipe()
    {
        DishType = DishType.Undetermined;
        baseIngredients = new();
        extraIngredients = new();
    }

    public Recipe(DishType dishType, IngredientData[] baseIngredients)
    {
        DishType = dishType;
        this.baseIngredients = new(baseIngredients);
        extraIngredients = new();
    }

    public Recipe(DishType dishType, IngredientData[] baseIngredients, IngredientData[] extraIngredients)
    {
        DishType = dishType;
        this.baseIngredients = new(baseIngredients);
        this.extraIngredients = new(extraIngredients);
    }

    // For merging ingredients on a plate
    public bool TryMergeIngredient(IngredientData newIngredient)
    {
        if (newIngredient.Type == IngredientType.None) return false;

        if (AlreadyContainsIngredient(newIngredient)) return false;

        // If the recipe is already matched and finished, check for the extra ingredients
        if (RecipeIsFinished())
        {
            bool compatibleExtra = matchedRecipe.ExtraIngredients.Contains(newIngredient);

            if (compatibleExtra)
                extraIngredients.Add(newIngredient);

            return compatibleExtra;
        }

        possibleRecipes ??= new(RecipesManager.Instance.DishToRecipe.Keys);

        // Check if the new ingredient is present in any of the possible recipes
        bool ingredientAccepted = possibleRecipes.Any(dish =>
        {
            RecipeScriptableObject recipeSO = RecipesManager.Instance.DishToRecipe[dish];
            return recipeSO.RequiredIngredients.Contains(newIngredient);
        });

        if (ingredientAccepted)
        {
            baseIngredients.Add(newIngredient);
            FilterPossibleRecipes();
        }

        return ingredientAccepted;
    }

    // For adding ingredients to a pan or pot (can't hold more than one)
    public bool TryAddIngredient(IngredientData newIngredient, UtensilType uType)
    {
        if (newIngredient.Type == IngredientType.None) return false;

        if (baseIngredients.Count >= 1 || AlreadyContainsIngredient(newIngredient)) return false;

        IngredientData[] compatibleIngredients;
        if (uType == UtensilType.Pan)
        {
            compatibleIngredients = RecipesManager.Instance.PanAcceptedIngredients;
        }
        else if (uType == UtensilType.Pot)
        {
            compatibleIngredients = RecipesManager.Instance.PotAcceptedIngredients;
        }
        else
        {
            Debug.LogError($"Failed to add ingredient to recipe: Found not valid utensil type ({uType})");
            return false;
        }

        bool ingredientAccepted = compatibleIngredients.Contains(newIngredient);

        if (ingredientAccepted)
            baseIngredients.Add(newIngredient);


        return ingredientAccepted;
    }

    public List<IngredientData> GetBaseIngredients()
    {
        return baseIngredients;
    }

    public List<IngredientData> GetExtraIngredients()
    {
        return extraIngredients;
    }

    public bool Matches(Recipe recipe)
    {
        if (recipe.DishType != DishType) return false;

        bool baseMatch = new HashSet<IngredientData>(baseIngredients)
                         .SetEquals(recipe.baseIngredients);

        bool extraMatch = new HashSet<IngredientData>(extraIngredients)
                          .SetEquals(recipe.extraIngredients);

        return baseMatch && extraMatch;
    }

    #region Helper Methods

    private bool AlreadyContainsIngredient(IngredientData newIngredient)
    {
        return baseIngredients.Contains(newIngredient) || extraIngredients.Contains(newIngredient);
    }

    // Filter recipes based on the current base ingredients
    private void FilterPossibleRecipes()
    {
        possibleRecipes.RemoveAll(dish => !IsCompatibleDish(dish));

        if (possibleRecipes.Count == 1)
        {
            DishType = possibleRecipes.First();
            matchedRecipe = RecipesManager.Instance.DishToRecipe[DishType];
        }
    }

    // Helper method to check a dish is compatible with the current base ingredients
    private bool IsCompatibleDish(DishType dish)
    {
        RecipeScriptableObject recipe = RecipesManager.Instance.DishToRecipe[dish];

        if (recipe == null)
        {
            Debug.LogError($"Recipe for dish {dish} not found in the dictionary.");
            return false;
        }

        return baseIngredients.All(i => recipe.RequiredIngredients.Contains(i));
    }

    private bool RecipeIsFinished()
    {
        return matchedRecipe && matchedRecipe.RequiredIngredients.Count() == baseIngredients.Count;
    }

    public bool TryAddExtra(IngredientData newExtra)
    {
        if (extraIngredients.Contains(newExtra))
            return false;

        extraIngredients.Add(newExtra);

        return true;
    }

    public int GetTotalIngredients()
    {
        return baseIngredients.Count + extraIngredients.Count;
    }

    public IngredientType[] GetAllIngredients()
    {
        return baseIngredients.Concat(extraIngredients)
            .Select(data => data.Type)
            .ToArray();
    }

    #endregion

    public override string ToString()
    {
        return $"DishType: {DishType}" +
            $" | Base Ingredients {string.Join(", ", baseIngredients)}" +
            $" | Extra Ingredients {string.Join(", ", extraIngredients)}";
    }
}
