using UnityEngine;
using Unity.Netcode;

public class IngredientBehaviour : PickableItemBehaviour
{
    private Renderer objRenderer;

    [field: Header("Type")]
    [field: SerializeField] public IngredientType Type { get; private set; }

    [Header("Attributes")]

    [SerializeField] private float requiredCookingTime = 2.0f;
    [SerializeField] private float requiredBurnTime = 3.0f;
    [SerializeField] private float cookedTime = 0f;

    [SerializeField] private float requiredCutTime = 3.0f;
    [SerializeField] private float cutTime = 0f;

    [Header("Visual")]
    [SerializeField] private Color cookedColor;
    [SerializeField] private Color burntColor;

    [Header("Cut Result")]
    [SerializeField] private GameObject cutPrefab;

    // NUEVO: marcar si el ingrediente ya está cortado
    [SerializeField] private bool isAlreadyCut = false;

    private NetworkVariable<bool> isCooked = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isBurnt = new NetworkVariable<bool>(false);

    private NetworkVariable<float> networkCookTime = new NetworkVariable<float>(0f);
    private NetworkVariable<float> networkCutTime = new NetworkVariable<float>(0f);

    public bool IsCooked => isCooked.Value;
    public bool IsBurnt => isBurnt.Value;

    // NUEVO: getter para saber si es cortado
    public bool IsAlreadyCut => isAlreadyCut;

    protected override void Awake()
    {
        base.Awake();
        objRenderer = GetComponentInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        isCooked.OnValueChanged += OnCookedChanged;
        isBurnt.OnValueChanged += OnBurntChanged;
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

    public void Cook(float cookTime)
    {
        if (!IsServer)
        {
            CookRpc(cookTime);
            return;
        }

        if (IsBurnt) return;

        cookedTime += cookTime;
        networkCookTime.Value = cookedTime;

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

    public void Cut(float cuttingTime)
    {
        if (!IsServer)
        {
            CutRpc(cuttingTime);
            return;
        }

        cutTime += cuttingTime;
        networkCutTime.Value = cutTime;
    }

    [Rpc(SendTo.Server)]
    private void CutRpc(float cuttingTime)
    {
        Cut(cuttingTime);
    }

    public float GetCookProgress()
    {
        float currentTime = IsServer ? cookedTime : networkCookTime.Value;
        return currentTime / (IsCooked ? requiredBurnTime : requiredCookingTime);
    }

    public float GetCutProgress()
    {
        float currentTime = IsServer ? cutTime : networkCutTime.Value;
        return currentTime / requiredCutTime;
    }

    public GameObject GetCutPrefab()
    {
        return cutPrefab;
    }

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

        return new IngredientData(Type, state);
    }
}