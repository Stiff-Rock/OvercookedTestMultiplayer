using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KitchenOrder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image recipeImage;
    [SerializeField] private TextMeshProUGUI dishNameText;
    [SerializeField] private Image lifetimeProgressBar;
    private Color initialBarColor;

    [SerializeField] private GameObject ingredientTagPrefab;
    [SerializeField] private GameObject tagPrefab;
    [SerializeField] private Transform ingredientsRow;

    [Header("Properties")]
    [SerializeField] private float maxLifespan;
    [field: SerializeField] public float Lifespan { get; private set; }
    [field: SerializeField] public Recipe Recipe { get; private set; }

    [Header("Events")]
    public UnityEvent OnExpire = new();

    // Flags
    private bool hasUI;

    private void Awake()
    {
        hasUI = lifetimeProgressBar != null;

        if (hasUI)
            initialBarColor = lifetimeProgressBar.color;

        enabled = false;
    }

    public void Initialize(Recipe recipe, float lifespan, RecipesVisuals recipeVisualsSO)
    {
        Recipe = recipe;
        maxLifespan = lifespan;
        Lifespan = lifespan;

        if (hasUI)
        {
            recipeImage.sprite = recipeVisualsSO.GetSprite(recipe.DishType);
            dishNameText.SetText(recipe.DishType.ToString());

            foreach (IngredientData ingredientData in recipe.GetAllIngredientData())
            {
                Sprite ingredientSprite = recipeVisualsSO.GetSprite(ingredientData.Type);
                GameObject ingredientTagObj = Instantiate(ingredientTagPrefab, ingredientsRow);
                ingredientTagObj.GetComponentInChildren<Image>().sprite = ingredientSprite;

                Transform utensilTransform = ingredientTagObj.transform.GetChild(1);

                if (ingredientData.State.HasFlag(IngredientState.Cut))
                {
                    Sprite cutSprite = recipeVisualsSO.GetSprite(UtensilType.Knife);

                    Image cutImg = Instantiate(tagPrefab, utensilTransform).GetComponent<Image>();
                    cutImg.sprite = cutSprite;
                }

                if (ingredientData.State.HasFlag(IngredientState.Cooked))
                {
                    IngredientData rawIngredient = ingredientData;
                    rawIngredient.State &= ~IngredientState.Cooked;
                    rawIngredient.State |= IngredientState.Raw;

                    UtensilType uType;
                    if (RecipesManager.Instance.PanAcceptedIngredients.Contains(rawIngredient))
                    {
                        uType = UtensilType.Pan;
                    }
                    else if (RecipesManager.Instance.PotAcceptedIngredients.Contains(rawIngredient))
                    {
                        uType = UtensilType.Pot;
                    }
                    else
                    {
                        Debug.LogError($"Ingredient {rawIngredient} is not accepted by any utensil");
                        return;
                    }

                    Sprite cookSprite = recipeVisualsSO.GetSprite(uType);
                    Image utensilImg = Instantiate(tagPrefab, utensilTransform).GetComponent<Image>();
                    utensilImg.sprite = cookSprite;
                }
            }
        }

        enabled = true;
    }

    private void Update()
    {
        Lifespan -= Time.deltaTime;

        if (hasUI)
        {
            float progress = Mathf.Clamp01(Lifespan / maxLifespan);
            lifetimeProgressBar.fillAmount = progress;
            lifetimeProgressBar.color = Color.Lerp(Color.red, initialBarColor, progress);
        }

        if (Lifespan <= 0)
        {
            Lifespan = 0;

            OnExpire.Invoke();
            enabled = false;
        }
    }
}