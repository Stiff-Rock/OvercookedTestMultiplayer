using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Kitchen/Recipe")]
public class RecipeScriptableObject : ScriptableObject
{
    [Header("Type")]
    public DishType dishType;

    [Header("Ingredients")]
    public IngredientType[] requiredIngredients;
    public IngredientType[] extraIngredients;
    
    [Header("Visual")]
    public GameObject resultPrefab;
}
