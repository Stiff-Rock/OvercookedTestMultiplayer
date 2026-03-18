using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // State
    private Dictionary<ulong, string> connectedClientsDict = new();
    private NetworkList<ulong> connectedClientIds = new();
    private NetworkList<FixedString32Bytes> connectedClientNames = new();

    #region Set-Up

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += AddClientToList;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkSpawn()
    {
        connectedClientNames.OnListChanged += _ => UpdateLobbyInfo();

        if (IsServer) AddHostToList();

        UpdateLobbyInfo();
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        base.OnDestroy();
    }

    #endregion

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int index = connectedClientIds.IndexOf(clientId);
        if (index != -1)
        {
            connectedClientIds.Remove(clientId);
            connectedClientNames.RemoveAt(index);
        }
    }

    private void AddHostToList()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        ulong hostId = NetworkManager.Singleton.LocalClientId;

        if (connectedClientIds.Contains(hostId)) return;

        string hostName = string.IsNullOrWhiteSpace(MenuManager.Instance.HostPlayerNameIF.text)
            ? Environment.UserName
            : MenuManager.Instance.HostPlayerNameIF.text;

        connectedClientsDict.Add(hostId, hostName);
        connectedClientIds.Add(hostId);
        connectedClientNames.Add(hostName);
    }

    private void AddClientToList(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (connectedClientIds.Contains(clientId)) return;

        if (connectedClientsDict.TryGetValue(clientId, out string playerName))
        {
            connectedClientIds.Add(clientId);
            connectedClientNames.Add(playerName);
        }
    }

    public void AddClientToDict(ulong clientId, string playerName)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (connectedClientIds.Contains(clientId)) return;
        connectedClientsDict.Add(clientId, playerName);
    }

    private void UpdateLobbyInfo()
    {
        MenuManager.Instance.LobbySubtitle.SetText($"Connected users ({connectedClientNames.Count}/4)");

        for (int i = 0; i < MenuManager.Instance.PlayerNameTexts.Length; i++)
        {
            TextMeshProUGUI playerNameText = MenuManager.Instance.PlayerNameTexts[i];
            if (!playerNameText) return;

            if (i < connectedClientNames.Count)
            {
                playerNameText.text = connectedClientNames[i].ToString();
                playerNameText.gameObject.SetActive(true);
            }
            else
            {
                playerNameText.gameObject.SetActive(false);
            }
        }
    }

    public string GetClientPlayerName(ulong clientId)
    {
        if (connectedClientsDict.TryGetValue(clientId, out string playerName)) return playerName;
        else return $"Unkown {clientId}";
    }
}
