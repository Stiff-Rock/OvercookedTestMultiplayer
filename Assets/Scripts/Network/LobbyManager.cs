using System;
using Unity.Collections;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour
{
    // State
    private NetworkList<ulong> connectedClientIds;
    private NetworkList<FixedString32Bytes> connectedClientNames;

    #region Set-Up

    private void Awake()
    {
        connectedClientIds = new();
        connectedClientNames = new();
    }

    private void Start()
    {
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

    // BUG: ON CLIENT IT SHOWS AS IF IT JOINED TWICE
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

        connectedClientIds.Add(hostId);
        connectedClientNames.Add(hostName);
    }

    public void AddClientToList(ulong clientId, string playerName)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (connectedClientIds.Contains(clientId)) return;

        connectedClientIds.Add(clientId);
        connectedClientNames.Add(playerName);
    }

    private void UpdateLobbyInfo()
    {
        MenuManager.Instance.LobbySubtitle.SetText($"Connected users ({connectedClientNames.Count}/4)");

        for (int i = 0; i < MenuManager.Instance.PlayerNameTexts.Length; i++)
        {
            if (i < connectedClientNames.Count)
            {

                MenuManager.Instance.PlayerNameTexts[i].text = connectedClientNames[i].ToString();
                MenuManager.Instance.PlayerNameTexts[i].gameObject.SetActive(true);
            }
            else
            {
                MenuManager.Instance.PlayerNameTexts[i].gameObject.SetActive(false);
            }
        }
    }
}
