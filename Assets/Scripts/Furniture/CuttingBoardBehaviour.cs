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

            SpawnCutIngredient();

            SetSliderActiveClientRpc(false);

            enabled = false;

            currentPlayer.ToggleActive(true);
        }
    }

    public override void OnInteract(PlayerController playerController)
    {
        base.OnInteract(playerController);

        if (!IsServer)
        {
            InteractServerRpc(playerController.NetworkObjectId);
            return;
        }

        
        if (placedIngredient && placedIngredient.IsAlreadyCut)
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

    private void SpawnCutIngredient()
    {
        if (!IsServer) return;

        GameObject prefab = placedIngredient.GetCutPrefab();

        if (prefab == null)
        {
            Debug.LogError("Cut prefab missing");
            return;
        }

       
        GameObject newObj = Instantiate(prefab, placeArea.transform);
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;

       
        IngredientBehaviour newIngredient = newObj.GetComponent<IngredientBehaviour>();
        if (newIngredient != null)
        {
            
            typeof(IngredientBehaviour)
                .GetField("isAlreadyCut", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(newIngredient, true);
        }

        NetworkObject netObj = newObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        PickableItemBehaviour newItem = newObj.GetComponent<PickableItemBehaviour>();

        
        newItem.NetworkSetParent(placeArea.transform, false);

        placedIngredient.NetworkObject.Despawn(true);

        PlacedItem = newItem;
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