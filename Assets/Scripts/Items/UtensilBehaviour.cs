using UnityEngine;

// TODO: UtensilBehaviour (Pots, Plates, Pans)
public class UtensilBehaviour : PickableItemBehaviour
{
    [SerializeField] private UtensilType utensilType;
    [SerializeField] private Recipe currentRecipe;

    private float stackHeightPos = 0f;

    protected override void Awake()
    {
        base.Awake();
        currentRecipe = new Recipe();
    }

    // Make a list of ingredients that the utensil accepts
    // (for example, a Pan would be able to hold meat but a Pot could not,
    // a plate could hold basically everything to be able to create the recipes)
    // ScriptablesObjects para el recetario

    // En caso del plato, una vez haya almacenado un ingrediente, eso hará 
    // que solo se puedan coger/sumar otros ingredientes compatibles.

    // Las ollas y sartenes solo podran almacenar un ingrediente a la vez
    // y pueden vertir su contenido sobre un plato, a no ser que esté
    // a medio cocinar

    // Todos pueden vertir su contenido en la basura en cualquier momento.

    // Las ollas, platos y sartenes no se pueden meter en el microondas

    // Las ollas y sartenes se usan poneindolas sobre una Stove

    // QUIZAS DEBE SER CADA INGREDIENTE EL QUE SEPA SOBRE QUE UTENSILIOS SE PUEDE PONER

    public void EmptyUtensil()
    {
        currentRecipe = new Recipe();
    }

    public bool TryAddIngredient(IngredientBehaviour ingredientItem)
    {
        bool added = currentRecipe.TryAddIngredient(ingredientItem.IngredientType);

        if (added)
        {
            ingredientItem.gameObject.transform.SetParent(transform, false);

            Renderer meshRenderer = ingredientItem.GetComponentInChildren<Renderer>();

            float realHeight = meshRenderer.bounds.size.y;
            stackHeightPos += realHeight / 2f;

            ingredientItem.gameObject.transform.position += Vector3.up * stackHeightPos;
        }

        return added;
    }
}