using System;
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
    [Header("References")]
    private UnityTransport transport;
    [SerializeField] private SceneAsset gameScene;

    private void Awake()
    {
        transport = GetComponent<UnityTransport>();
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
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
        Application.Quit();
    }

    #region Callbacks

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Debug.Log($"Cliente conectado con Id {clientId}");

        if (SceneManager.GetActiveScene().name == gameScene.name)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameScene.name, LoadSceneMode.Single);
        }
    }

    private void OnServerStarted()
    {
        Debug.Log($"Servidor iniciado");
        NetworkManager.Singleton.SceneManager.LoadScene(gameScene.name, LoadSceneMode.Single);
    }

    #endregion
}