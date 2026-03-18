using System;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class Launcher : MonoBehaviour
{
    public static bool wasDisconnected;

    [Header("References")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private string lobbySceneName;
    [SerializeField] private GameObject lobbyManagerPrefab;

    private GameObject debugConsole;
    private LobbyManager lobbyManager;

    #region Set-Up

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = OnApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;

            if (NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }

    #endregion

    #region Network Actions

    public void StartClient()
    {
        try
        {
            string playername = string.IsNullOrWhiteSpace(MenuManager.Instance.ClientPlayerNameIF.text)
                ? Environment.UserName
                : MenuManager.Instance.ClientPlayerNameIF.text;

            byte[] payload = Encoding.ASCII.GetBytes(playername);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
            MenuManager.Instance.LoadingTextObj.SetActive(true);
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            MenuManager.Instance.LoadingTextObj.SetActive(false);
            MenuManager.Instance.MainMenuObj.SetActive(true);
            Debug.LogError($"Could not start client: {e.Message}");
            NetworkManager.Singleton.Shutdown();
        }

    }

    public void StartHost()
    {
        try
        {
            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            MenuManager.Instance.MainMenuObj.SetActive(true);
            Debug.LogError($"Could not start host: {e.Message}");
            NetworkManager.Singleton.Shutdown();
        }
    }

    public void Shutdown()
    {
        try
        {
            NetworkManager.Singleton.Shutdown();
            if (lobbyManager) Destroy(lobbyManager);
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not shutdown network: {e.Message}");
        }
    }

    #endregion

    #region User Actions

    public void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Callbacks

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente conectado con Id {clientId}");

        if (!NetworkManager.Singleton.IsServer)
        {
            MenuManager.Instance.LobbyMenuStartButton.SetActive(false);
            MenuManager.Instance.LoadingTextObj.SetActive(false);
            MenuManager.Instance.LobbyMenuObj.SetActive(true);
        }
        else
        {
            MenuManager.Instance.LobbyMenuStartButton.SetActive(true);
        }

        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Se perdió la conexión con el servidor.");
            wasDisconnected = true;
            SceneManager.LoadScene(lobbySceneName);
            return;
        }
    }

    private void OnApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        bool approved = NetworkManager.Singleton.ConnectedClients.Count < 4;
        response.Approved = approved;
        response.CreatePlayerObject = false;
        if (approved)
        {
            string playerName = Encoding.ASCII.GetString(request.Payload);

            if (playerName == null || string.IsNullOrWhiteSpace(playerName))
                playerName = $"Unknown {request.ClientNetworkId}";

            if (lobbyManager && request.ClientNetworkId != NetworkManager.Singleton.LocalClientId)
                lobbyManager.AddClientToDict(request.ClientNetworkId, playerName);
        }
        response.Pending = false;
    }

    private void OnServerStarted()
    {
        Debug.Log($"Servidor iniciado");

        if (lobbyManager != null) Destroy(lobbyManager);
        GameObject lobbyManagerObj = Instantiate(lobbyManagerPrefab);
        lobbyManager = lobbyManagerObj.GetComponent<LobbyManager>();
        lobbyManagerObj.GetComponent<NetworkObject>().Spawn(true);

        MenuManager.Instance.LobbyMenuObj.SetActive(true);
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private void OnServerStopped(bool closedCorrectly)
    {
        Debug.Log("Servidor parado");

        if (debugConsole) debugConsole.SetActive(false);
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.Load)
        {
            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.LobbyMenuObj.SetActive(false);
                MenuManager.Instance.LoadingTextObj.SetActive(true);
            }
        }
    }

    #endregion
}