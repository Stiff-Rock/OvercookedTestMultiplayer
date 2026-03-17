using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UtensilBehaviour : PickableItemBehaviour
{
    [field: SerializeField] public UtensilType UtensilType { get; private set; }
    [field: SerializeField] public Recipe CurrentRecipe { get; private set; }

    [SerializeField] private Transform utensilContentTransform;
    [SerializeField] private List<IngredientBehaviour> heldIngredients;

    private float stackHeightPos = 0f;

    protected override void Awake()
    {
        base.Awake();
        CurrentRecipe = new Recipe();
        heldIngredients = new List<IngredientBehaviour>();
    }

    public void EmptyUtensil()
    {
        CurrentRecipe = new Recipe();
        DeleteIngredients();
        EmptyUtensil_ClientRpc();
    }

    [ClientRpc]
    private void EmptyUtensil_ClientRpc()
    {
        CurrentRecipe = new Recipe();
        DeleteIngredients();
    }

    public bool TryAddIngredient(IngredientBehaviour ingredientItem)
    {
        bool added;
        if (UtensilType == UtensilType.Plate)
        {
            added = CurrentRecipe.TryMergeIngredient(ingredientItem.ToIngredientData());
        }
        else if (UtensilType == UtensilType.Pan || UtensilType == UtensilType.Pot)
        {
            added = CurrentRecipe.TryAddIngredient(ingredientItem.ToIngredientData(), UtensilType);
        }
        else
        {
            Debug.LogError($"Utensil '{gameObject.name}' has no valid UtensilType (found {UtensilType})");
            added = false;
        }

        if (added) PlaceIngredientIntoUtensil(ingredientItem);

        TryAddIngredient_ClientRpc(ingredientItem.NetworkObjectId);

        return added;
    }

    [ClientRpc]
    private void TryAddIngredient_ClientRpc(ulong ingredientItemId)
    {
        if (IsServer) return;

        IngredientBehaviour ingredientItem =
            NetHelpers.GetNetComponent<IngredientBehaviour>(ingredientItemId);

        bool added;
        if (UtensilType == UtensilType.Plate)
        {
            added = CurrentRecipe.TryMergeIngredient(ingredientItem.ToIngredientData());
        }
        else if (UtensilType == UtensilType.Pan || UtensilType == UtensilType.Pot)
        {
            added = CurrentRecipe.TryAddIngredient(ingredientItem.ToIngredientData(), UtensilType);
        }
        else
        {
            Debug.LogError($"Utensil '{gameObject.name}' has no valid UtensilType (found {UtensilType})");
            added = false;
        }

        if (added) PlaceIngredientIntoUtensil(ingredientItem);
    }

    private void PlaceIngredientIntoUtensil(IngredientBehaviour ingredientItem)
    {
        StackIngredients(ingredientItem);

        InteractiveAppliance appliance = transform.parent.GetComponent<InteractiveAppliance>();

        if (appliance)
            appliance.OnPlacedItemChanged();
    }

    public IngredientBehaviour RemoveIngredient()
    {
        IngredientBehaviour ingB = heldIngredients[0];

        List<IngredientData> ingredients = CurrentRecipe.GetBaseIngredients();
        ingredients.RemoveAt(0);

        heldIngredients.RemoveAt(0);

        InteractiveAppliance appliance = transform.parent.GetComponent<InteractiveAppliance>();
        if (appliance)
            appliance.OnPlacedItemChanged();

        RemoveIngredient_ClientRpc();

        return ingB;
    }

    [ClientRpc]
    private void RemoveIngredient_ClientRpc()
    {
        if (IsServer) return;

        List<IngredientData> ingredients = CurrentRecipe.GetBaseIngredients();
        ingredients.RemoveAt(0);

        heldIngredients.RemoveAt(0);

        InteractiveAppliance appliance = transform.parent.GetComponent<InteractiveAppliance>();
        if (appliance)
            appliance.OnPlacedItemChanged();
    }

    public bool CanTakeIngredient()
    {
        return (UtensilType == UtensilType.Pan || UtensilType == UtensilType.Pot)
            && heldIngredients.Count > 0
            && heldIngredients[0]
            && heldIngredients[0].IsCooked
            || (!heldIngredients[0].IsBurnt && heldIngredients[0].GetCookProgress() <= 0);
    }

    public IngredientBehaviour PeekIngredient()
    {
        if (heldIngredients.Count <= 0) return null;
        return heldIngredients[0];
    }

    #region Helper Methods

    private void DeleteIngredients()
    {
        foreach (IngredientBehaviour ingredient in heldIngredients)
        {
            Destroy(ingredient.gameObject);
        }

        heldIngredients.Clear();
    }

    private void StackIngredients(IngredientBehaviour ingredientItem)
    {
        if (!utensilContentTransform)
        {
            Debug.LogWarning("Cannot StackIngredients since utensilContentTransform is null");
            return;
        }

        if (IsServer)
            ingredientItem.NetworkSetParent(utensilContentTransform, false);

        Renderer meshRenderer = ingredientItem.GetComponentInChildren<Renderer>();
        float itemHeight = meshRenderer.bounds.size.y;

        ingredientItem.gameObject.transform.localPosition = new Vector3(
                    0,
            stackHeightPos + (itemHeight / 2f),
                    0
        );

        stackHeightPos += itemHeight;

        heldIngredients.Add(ingredientItem);
    }

    #endregion
}