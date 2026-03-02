using UnityEngine;

[RequireComponent(typeof(ProgressSliderBehaviour))]
public class StoveBehaviour : InteractiveAppliance
{
    private ProgressSliderBehaviour progressBar;

    [Header("Cooking Settings")]
    [Range(0.5f, 2f)]
    [SerializeField] private float cookingPower = 1.0f;

    private void Awake()
    {
        progressBar = GetComponent<ProgressSliderBehaviour>();
    }

    protected override void Start()
    {
        base.Start();
        enabled = placedUtensil && placedUtensil.PeekIngredient();
    }

    private void Update()
    {
        if (!placedUtensil) return;

        if (IsFinishedCooking())
        {
            ToggleActiveStove(false);
        }
        else
        {
            placedUtensil.PeekIngredient().Cook(Time.deltaTime * cookingPower);
            progressBar.UpdateProgressBar(placedUtensil.PeekIngredient().GetCookProgress());
        }
    }

    public override void OnPlacedItemChanged()
    {
        if (!placedUtensil || placedUtensil.UtensilType == UtensilType.Plate)
        {
            ToggleActiveStove(false);
            return;
        }


        bool isCooking = placedUtensil.PeekIngredient() && !placedUtensil.PeekIngredient().IsBurnt;
        ToggleActiveStove(isCooking);
    }

    private void ToggleActiveStove(bool active)
    {
        progressBar.SetActive(active);
        enabled = active;
    }

    private bool IsFinishedCooking()
    {
        return cookingPower > 0.7
            && placedUtensil.PeekIngredient().IsBurnt
            || cookingPower <= 0.7
            && placedUtensil.PeekIngredient().IsCooked;
    }
}