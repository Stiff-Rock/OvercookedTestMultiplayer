using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class PlayerNameTag : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerTagCanvas;
    [SerializeField] private TextMeshProUGUI playerNameTagText;

    private NetworkVariable<FixedString32Bytes> playerName = new();

    [Header("Settings")]
    [SerializeField] private Vector3 targetRotation = new(45, 0, 0);

    public override void OnNetworkSpawn()
    {
        playerName.OnValueChanged += OnNameChanged;
        UpdateTag(playerName.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= OnNameChanged;
    }

    private void Update()
    {
        playerTagCanvas.transform.rotation = Quaternion.Euler(targetRotation);
    }

    private void OnNameChanged(FixedString32Bytes _, FixedString32Bytes newValue)
    {
        UpdateTag(newValue.ToString());
    }

    private void UpdateTag(string name)
    {
        if (playerNameTagText != null)
            playerNameTagText.SetText(name);
    }

    public void SetPlayerTag(string playerTag)
    {
        if (IsServer) playerName.Value = playerTag;
    }
}
