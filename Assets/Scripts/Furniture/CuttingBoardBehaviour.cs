using UnityEngine;

// TODO: CuttingBoardBehaviour

[RequireComponent(typeof(ProgressSliderBehaviour))]
public class CuttingBoardBehaviour : InteractiveAppliance
{
    private ProgressSliderBehaviour progressBar;

    private void Awake()
    {
        progressBar = GetComponent<ProgressSliderBehaviour>();
    }

    public override void OnInteract()
    {
    }
}