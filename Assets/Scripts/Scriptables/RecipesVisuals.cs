using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipesVisuals", menuName = "Kitchen/RecipesVisuals")]
public class RecipesVisuals : ScriptableObject
{
    [SerializeField] private List<IngredientUIInfo> ingredientTable;
    [SerializeField] private List<DishUIInfo> dishTable;

    public Sprite GetSprite(IngredientType type)
    {
        return ingredientTable.Find(x => x.type == type).sprite;
    }

    public Sprite GetSprite(DishType type)
    {
        return dishTable.Find(x => x.type == type).sprite;
    }
}

[Serializable]
public struct IngredientUIInfo
{
    public IngredientType type;
    public Sprite sprite;
}

[Serializable]
public struct DishUIInfo
{
    public DishType type;
    public Sprite sprite;
}