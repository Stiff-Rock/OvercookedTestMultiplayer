using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkObject))]
public class GameController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Transform[] spawnPositions;
    private PlayerController[] players;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 600.0f;
    [SerializeField] private float orderingRate = 10.0f;
    [SerializeField] private float orderTimer;

    // Events
    public UnityEvent onCreateOrder;
    public UnityEvent onGameOver;

    private float nextSyncTime;

    private void Awake()
    {
        enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        enabled = IsServer;

        if (IsServer) SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        IReadOnlyList<ulong> connectedClientsIds = NetworkManager.Singleton.ConnectedClientsIds;
        for (int i = 0; i < connectedClientsIds.Count; i++)
        {
            ulong clientId = connectedClientsIds[i];
            Transform spawnPosition = spawnPositions[i];

            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition.position, Quaternion.identity, spawnPosition);

            string playerName = LobbyManager.Instance.GetClientPlayerName(clientId);
            playerInstance.GetComponentInChildren<PlayerNameTag>().SetPlayerTag(playerName);

            NetworkObject playerNetworkObject = playerInstance.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnAsPlayerObject(clientId);
        }
    }

    private void Start()
    {
        if (!IsServer) return;

        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");
        players = playerObjs
            .Select(p => p.GetComponent<PlayerController>())
            .Where(p => p != null)
            .ToArray();

        onCreateOrder.Invoke();
    }

    private void Update()
    {
        UpdateGameTime();
        OrderTick();
    }

    private void UpdateGameTime()
    {
        gameDuration -= Time.deltaTime;

        if (Time.time >= nextSyncTime)
        {
            UpdateGameTime_ClientRpc(gameDuration);
            nextSyncTime = Time.time + 1f;
        }

        if (gameDuration <= 0)
        {
            gameDuration = 0;
            DisablePlayers();
            ScoreManager.Instance.ShowFinalScore();
            enabled = false;
        }
    }

    [ClientRpc]
    private void UpdateGameTime_ClientRpc(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        timerText.SetText(string.Format("{0:D2}:{1:D2}", minutes, seconds));

        if (remainingTime <= 0)
        {
            onGameOver.Invoke();
            enabled = false;
        }
    }

    private void DisablePlayers()
    {
        foreach (PlayerController player in players)
        {
            player.ToggleActive(false);
        }
    }

    private void OrderTick()
    {
        orderTimer += Time.deltaTime;

        if (orderTimer >= orderingRate)
        {
            orderTimer = 0;
            onCreateOrder.Invoke();
        }
    }
}