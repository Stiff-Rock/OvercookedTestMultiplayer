using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuBehaviour : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Update()
    {
        CheckTabMenu();
    }

    private void CheckTabMenu()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            canvasGroup.alpha = canvasGroup.alpha == 1 ? 0 : 1;
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
        canvasGroup.alpha = 0;
    }
}
