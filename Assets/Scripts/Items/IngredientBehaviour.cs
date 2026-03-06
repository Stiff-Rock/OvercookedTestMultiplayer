using UnityEngine;
using Unity.Netcode;

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

    // Network synced flags
    private NetworkVariable<bool> isCooked = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isBurnt = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isCut = new NetworkVariable<bool>(false);

    // Network synced progress
    private NetworkVariable<float> networkCookTime = new NetworkVariable<float>(0f);
    private NetworkVariable<float> networkCutTime = new NetworkVariable<float>(0f);

    // Flags
    public bool IsCooked => isCooked.Value;
    public bool IsBurnt => isBurnt.Value;
    public bool IsCut => isCut.Value;

    protected override void Awake()
    {
        base.Awake();
        objRenderer = GetComponentInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        isCooked.OnValueChanged += OnCookedChanged;
        isBurnt.OnValueChanged += OnBurntChanged;
        isCut.OnValueChanged += OnCutChanged;
    }

    private void OnCookedChanged(bool prev, bool value)
    {
        if (value)
            objRenderer.material.color = cookedColor;
    }

    private void OnBurntChanged(bool prev, bool value)
    {
        if (value)
            objRenderer.material.color = burntColor;
    }

    private void OnCutChanged(bool prev, bool value)
    {
        if (value)
            objRenderer.material.color = cutColor;
    }

    // --- Cooking ---
    public void Cook(float cookTime)
    {
        if (!IsServer)
        {
            CookRpc(cookTime);
            return;
        }

        if (IsBurnt) return;

        cookedTime += cookTime;
        networkCookTime.Value = cookedTime; // sincroniza con clientes

        if (!IsCooked && cookedTime >= requiredCookingTime && cookedTime < requiredBurnTime)
        {
            isCooked.Value = true;
            cookedTime = 0;
        }
        else if (IsCooked && cookedTime >= requiredBurnTime)
        {
            isBurnt.Value = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void CookRpc(float cookTime)
    {
        Cook(cookTime);
    }

    // --- Cutting ---
    public void Cut(float cuttingTime)
    {
        if (!IsServer)
        {
            CutRpc(cuttingTime);
            return;
        }

        if (IsCut) return;

        cutTime += cuttingTime;
        networkCutTime.Value = cutTime; // sincroniza con clientes

        if (cutTime >= requiredCutTime)
        {
            isCut.Value = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void CutRpc(float cuttingTime)
    {
        Cut(cuttingTime);
    }

    #region Getters

    public float GetCookProgress()
    {
        // usa NetworkVariable si no es servidor
        float currentTime = IsServer ? cookedTime : networkCookTime.Value;
        return currentTime / (IsCooked ? requiredBurnTime : requiredCookingTime);
    }

    public float GetCutProgress()
    {
        // usa NetworkVariable si no es servidor
        float currentTime = IsServer ? cutTime : networkCutTime.Value;
        return currentTime / requiredCutTime;
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
        if (IsBurnt)
            state = IngredientState.Burnt;
        else if (IsCooked)
            state = IngredientState.Cooked;
        else
            state = IngredientState.Raw;

        if (IsCut)
            state |= IngredientState.Cut;

        return new IngredientData(Type, state);
    }
}