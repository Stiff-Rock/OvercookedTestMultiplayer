using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuBehaviour : MonoBehaviour
{
    [SerializeField] Canvas Menu;

    private void Update()
    {
        tabMenu();
    }

    private void tabMenu()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Menu.gameObject.SetActive(true);
        }
    }

    public void returnLobby()
    {
        SceneManager.LoadScene("LobbyScene");
        Launcher.WasDisconnected = true;
        NetworkManager.Singleton.Shutdown();
    }
    public void closeCanvas()
    {
        Menu.gameObject.SetActive(false);
    }

}
