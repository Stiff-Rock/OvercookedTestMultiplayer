using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(ProgressSliderBehaviour))]
public class CuttingBoardBehaviour : InteractiveAppliance
{
    private ProgressSliderBehaviour progressBar;

    private bool isCutting;

    private void Awake()
    {
        progressBar = GetComponent<ProgressSliderBehaviour>();
        enabled = false;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (!placedIngredient || isCutting == false)
            return;

        progressBar.UpdateProgressBar(placedIngredient.GetCutProgress());
        placedIngredient.Cut(Time.deltaTime);

        UpdateProgressClientRpc(placedIngredient.GetCutProgress());

        if (placedIngredient.GetCutProgress() >= 1f)
        {
            isCutting = false;

            placedIngredient.SetIsCut();
            placedIngredient.NetworkSetParent(placeArea.transform, false);
            SetSliderActiveClientRpc(false);

            enabled = false;

            currentPlayer.ToggleActive(true);
        }
    }

    // TODO: NO SE TIENE QUE PODER PONER CUALQUIER INGREDIENTE
    public override void OnInteract(PlayerController playerController)
    {
        base.OnInteract(playerController);

        if (!IsServer)
        {
            InteractServerRpc(playerController.NetworkObjectId);
            return;
        }


        if (placedIngredient && placedIngredient.IsCut)
            return;

        if (placedIngredient)
        {
            isCutting = true;

            progressBar.SetActive(true);
            SetSliderActiveClientRpc(true);

            enabled = true;

            currentPlayer.ToggleActive(false);
        }
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(ulong playerId)
    {
        PlayerController player = NetHelpers.GetNetComponent<PlayerController>(playerId);
        OnInteract(player);
    }


    [ClientRpc]
    private void SetSliderActiveClientRpc(bool active)
    {
        progressBar.SetActive(active);
    }

    [ClientRpc]
    private void UpdateProgressClientRpc(float progress)
    {
        progressBar.UpdateProgressBar(progress);
    }
}