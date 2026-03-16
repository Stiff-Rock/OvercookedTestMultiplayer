using Unity.Netcode;
using UnityEngine;

public class IngredientBehaviour : PickableItemBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject normalModel;
    [SerializeField] private GameObject cutModel; 
    private MeshRenderer meshRenderer;

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

    [Header("State")]
    private readonly NetworkVariable<bool> isCooked = new(false);
    private readonly NetworkVariable<bool> isBurnt = new(false);
    [SerializeField] private bool canBeCut = true;
    [SerializeField] private bool isCut = false;

    private readonly NetworkVariable<float> networkCookTime = new(0f);
    private readonly NetworkVariable<float> networkCutTime = new(0f);

    public bool IsCooked => isCooked.Value;
    public bool IsBurnt => isBurnt.Value;
    public bool CanBeCut => canBeCut;
    public bool IsCut => isCut;

    protected override void Awake()
    {
        base.Awake();
        GameObject rendererOrigin = isCut ? cutModel : normalModel;
        meshRenderer = rendererOrigin.GetComponent<MeshRenderer>();

        if (isBurnt.Value) meshRenderer.materials[0].color = burntColor;
        else if (isCooked.Value) meshRenderer.materials[0].color = cookedColor;

        isCooked.OnValueChanged += (_, _) =>
        {
            meshRenderer.materials[0].color = cookedColor;
        };

        isBurnt.OnValueChanged += (_, _) =>
        {
            meshRenderer.materials[0].color = burntColor;
        };
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

        if(isCut)
            state |= IngredientState.Cut;

        return new IngredientData(Type, state);
    }

    public void SetIsCut()
    {
        isCut = true;
        normalModel.SetActive(false);
        cutModel.SetActive(true);
        meshRenderer = cutModel.GetComponent<MeshRenderer>();
        SetIsCut_ClientRpc();
    }

    [ClientRpc]
    private void SetIsCut_ClientRpc()
    {
        isCut = true;
        normalModel.SetActive(false);
        cutModel.SetActive(true);
        meshRenderer = cutModel.GetComponent<MeshRenderer>();
    }
}