using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Kitchen/Recipe")]
public class RecipeScriptableObject : ScriptableObject
{
    [Header("Type")]
    [field: SerializeField] public DishType DishType { get; private set; }

    [Header("Ingredients")]
    [field: SerializeField] public IngredientData[] RequiredIngredients { get; private set; }
    [field: SerializeField] public IngredientData[] ExtraIngredients { get; private set; }

    [Header("Visual")]
    [field: SerializeField] public GameObject ResultPrefab { get; private set; }

    public override string ToString()
    {
        return $"RequiredIngredients: {string.Join(",", RequiredIngredients)} " +
            $"|| ExtraIngredients: {string.Join(",", ExtraIngredients)}";
    }
}
