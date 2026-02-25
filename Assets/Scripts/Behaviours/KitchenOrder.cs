using System.Collections.Generic;
using TMPro;
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

    public void Initialize(Recipe recipe, float lifespan, IngredientVisuals ingredientVisualsSO)
    {
        Recipe = recipe;
        maxLifespan = lifespan;
        Lifespan = lifespan;

        if (hasUI)
        {
            dishNameText.SetText(recipe.DishType.ToString());

            foreach (IngredientType i in recipe.GetAllIngredients())
            {
                Sprite ingredientSprite = ingredientVisualsSO.GetSprite(i);

                Image tagImg = Instantiate(tagPrefab, ingredientsRow).GetComponent<Image>();

                tagImg.sprite = ingredientSprite;
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