using UnityEngine;

// TODO: Modelar ingredientes, ingredientes cortados, platos y hacer algo para mejorar el aspecto cocinado
public class IngredientBehaviour : PickableItemBehaviour
{
    // References
    private Renderer objRenderer;

    // Cooking parameters
    [field: Header("Type")]
    [field: SerializeField] public IngredientType Type { get; private set; }

    [Header("Attributes")]
    // Cooking
    [SerializeField] private float requiredCookingTime = 2.0f;
    [SerializeField] private float requiredBurnTime = 3.0f;
    [SerializeField] private float cookedTime = 0f;
    // Cutting
    [SerializeField] private float requiredCutTime = 3.0f;
    [SerializeField] private float cutTime = 0f;

    [Header("Visual")]
    [SerializeField] private Color cookedColor;
    [SerializeField] private Color burntColor;
    [SerializeField] private Color cutColor;

    // Flags
    public bool IsCooked { get; private set; }
    public bool IsBurnt { get; private set; }
    public bool IsCut { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        IsCooked = false;
        IsBurnt = false;
        IsCut = false;

        objRenderer = GetComponentInChildren<Renderer>();
    }

    public void Cook(float cookTime)
    {
        if (IsBurnt) return;

        cookedTime += cookTime;

        if (!IsCooked && cookedTime >= requiredCookingTime && cookedTime < requiredBurnTime)
        {
            IsCooked = true;
            objRenderer.material.color = cookedColor;
            cookedTime = 0;
        }
        else if (IsCooked && cookedTime >= requiredBurnTime)
        {
            IsBurnt = true;
            objRenderer.material.color = burntColor;
        }
    }

    public void Cut(float cuttingTime)
    {
        if (IsCut) return;

        cutTime += cuttingTime;

        if (cutTime >= requiredCutTime)
        {
            IsCut = true;
            objRenderer.material.color = cutColor;
        }
    }

    #region Getters

    public float GetCookProgress()
    {
        return cookedTime / (IsCooked ? requiredBurnTime : requiredCookingTime);
    }

    public float GetCutProgress()
    {
        return cutTime / requiredCutTime;
    }

    #endregion

    public IngredientData ToIngredientData()
    {
        if (Type == IngredientType.None)
        {
            Debug.LogError($"ToIngredientData() call failed: IngredientBehaviour '{gameObject.name}' IngredientType is none");
            return default;
        }

        IngredientState state;
        if (IsCooked)
            state = IngredientState.Cooked;
        else if (IsBurnt)
            state = IngredientState.Burnt;
        else
            state = IngredientState.Raw;

        if (IsCut)
            state |= IngredientState.Cut;

        return new IngredientData(Type, state);
    }
}
