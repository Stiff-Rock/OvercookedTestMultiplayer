using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class Launcher : MonoBehaviour
{
    [SerializeField] private SceneAsset gameScene;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

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
}