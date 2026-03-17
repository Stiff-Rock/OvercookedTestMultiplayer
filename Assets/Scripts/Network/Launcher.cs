using System;
using System.Text;
using TMPro;
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
    [SerializeField] private GameObject inGameDebugConsolePrefab;

    private GameObject debugConsole;

    [SerializeField] private TMP_InputField clientPlayerNameIF;

    [SerializeField] private GameObject loadingTextObj;
    [SerializeField] private GameObject mainMenuObj;
    [SerializeField] private GameObject lobbyMenuObj;

    private LobbyManager lobbyManager;

    [Header("Debug")]
    [SerializeField] private bool allowDebugConsole;

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
        }
    }

    #endregion

    #region Network Actions

    public void StartClient()
    {
        try
        {
            string playername = string.IsNullOrWhiteSpace(clientPlayerNameIF.text)
                ? Environment.UserName
                : clientPlayerNameIF.text;

            byte[] payload = System.Text.Encoding.ASCII.GetBytes(playername);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
            loadingTextObj.SetActive(true);
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            loadingTextObj.SetActive(false);
            mainMenuObj.SetActive(true);
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
            mainMenuObj.SetActive(true);
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
            loadingTextObj.SetActive(false);
            lobbyMenuObj.SetActive(true);
        }
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
    // BUG: ON CLIENT IT SHOWS AS IF IT JOINED TWICE
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
                lobbyManager.AddClientToList(request.ClientNetworkId, playerName);
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

        lobbyMenuObj.SetActive(true);

        if (allowDebugConsole && !debugConsole)
            debugConsole = Instantiate(inGameDebugConsolePrefab, Vector3.zero, Quaternion.identity);
        else if (allowDebugConsole && debugConsole)
            debugConsole.SetActive(true);
    }

    private void OnServerStopped(bool closedCorrectly)
    {
        Debug.Log("Servidor parado");

        if (debugConsole) debugConsole.SetActive(false);
    }

    #endregion
}