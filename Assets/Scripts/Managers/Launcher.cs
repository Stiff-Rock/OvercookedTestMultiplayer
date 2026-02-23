using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class Launcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private string gameSceneName;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    public void StartClient()
    {
        try
        {
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
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
            Debug.LogError($"Could not start host: {e.Message}");
            NetworkManager.Singleton.Shutdown();
        }
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #region Callbacks

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente conectado con Id {clientId}");
    }

    private void OnServerStarted()
    {
        Debug.Log($"Servidor iniciado");
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    #endregion
}