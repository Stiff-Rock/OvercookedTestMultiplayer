using UnityEngine;

public class UIDisconnection : MonoBehaviour
{  
        [SerializeField] private Canvas desconnectionLostCanvas;

        private void Start()
        {
            if (Launcher.wasDisconnected)
            {
            desconnectionLostCanvas.gameObject.SetActive(true);
            Launcher.wasDisconnected = false;
            }
        }
    public void closeCanvas()
    {
       desconnectionLostCanvas.gameObject.SetActive(false);
    }

}
