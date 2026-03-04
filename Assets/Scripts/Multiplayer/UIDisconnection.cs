using UnityEngine;

public class UIDisconnection : MonoBehaviour
{  
        [SerializeField] private Canvas desconnectionLostCanvas;

        private void Start()
        {
            if (Launcher.WasDisconnected)
            {
            desconnectionLostCanvas.gameObject.SetActive(true);
            Launcher.WasDisconnected = false;
            }
        }
    public void closeCanvas()
    {
       desconnectionLostCanvas.gameObject.SetActive(false);
    }

}
