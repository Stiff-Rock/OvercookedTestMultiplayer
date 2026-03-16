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

            TogglePlayerController(true);
        }
    }

    public override void OnInteract(PlayerController playerController)
    {
        if (placedIngredient && !placedIngredient.CanBeCut)
            return;

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

            TogglePlayerController(false);
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

    private void TogglePlayerController(bool active)
    {
        if (currentPlayer)
        {
            currentPlayer.ToggleActive(active);
            TogglePlayerController_ClientRpc(currentPlayer.NetworkObjectId, active);
        }
    }

    [ClientRpc]
    private void TogglePlayerController_ClientRpc(ulong playerNetId, bool active)
    {
        PlayerController pC = NetHelpers.GetNetComponent<PlayerController>(playerNetId);
        pC.ToggleActive(active);
    }
}