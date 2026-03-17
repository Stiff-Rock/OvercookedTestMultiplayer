using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ProgressSliderBehaviour))]
public class MicrowaveBehaviour : InteractiveAppliance
{
    private Animator animator;
    private ProgressSliderBehaviour progressBar;

    [Header("SFX")]
    private AudioSource audioSource;
    [SerializeField] private AudioClip oven;
    [SerializeField] private AudioClip ding;

    public bool isCooking = false;

    private void Awake()
    {
        progressBar = GetComponent<ProgressSliderBehaviour>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isCooking && placedIngredient)
        {
            progressBar.UpdateProgressBar(placedIngredient.GetCookProgress());
        }
    }

    public override void PlaceItem(PickableItemBehaviour newItem)
    {
        if (newItem is UtensilBehaviour) return;
        base.PlaceItem(newItem);
    }

    private IEnumerator Cook()
    {
        if (placedIngredient)
        {
            ToggleCookAnimation(true);

            SetSliderActiveClientRpc(true);

            if (!placedIngredient.IsCooked)
            {
                while (!placedIngredient.IsCooked)
                {
                    placedIngredient.Cook(Time.deltaTime);

                    UpdateProgressClientRpc(placedIngredient.GetCookProgress());

                    yield return null;
                }
            }
            else if (!placedIngredient.IsBurnt)
            {
                while (!placedIngredient.IsBurnt)
                {
                    placedIngredient.Cook(Time.deltaTime);

                    UpdateProgressClientRpc(placedIngredient.GetCookProgress());

                    yield return null;
                }
            }

            ToggleCookAnimation(false);

            SetSliderActiveClientRpc(false);
        }
    }

    private void ToggleCookAnimation(bool isCooking)
    {
        this.isCooking = isCooking;
        enabled = isCooking;

        progressBar.SetActive(isCooking);
        animator.SetBool("isCooking", isCooking);

        if (audioSource.isPlaying) audioSource.Stop();
        audioSource.PlayOneShot(isCooking ? oven : ding);
    }

    public override PickableItemBehaviour TakeItem()
    {
        return isCooking ? null : base.TakeItem();
    }

    public override void OnInteract(PlayerController playerController)
    {
        base.OnInteract(playerController);

        if (!IsServer)
        {
            InteractServerRpc(playerController.NetworkObjectId);
            return;
        }

        if (!isCooking && placedIngredient && !placedIngredient.IsBurnt)
            StartCoroutine(Cook());
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