using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuBehaviour : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    private bool active = false;

    private void Awake()
    {
        canvasGroup.interactable = active;
        canvasGroup.alpha = active ? 1 : 0;
    }

    private void Update()
    {
        CheckTabMenu();
    }

    private void CheckTabMenu()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            active = !active;
            canvasGroup.interactable = active;
            canvasGroup.alpha = active ? 1 : 0;
        }
    }

    public void ReturnToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
        Launcher.wasDisconnected = true;
        NetworkManager.Singleton.Shutdown();
    }

    public void CloseCanvas()
    {
        active = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0;
    }
}
