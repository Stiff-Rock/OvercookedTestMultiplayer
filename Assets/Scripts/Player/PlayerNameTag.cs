using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerTagCanvas;
    [SerializeField] private TextMeshProUGUI playerNameTagText;

    [Header("Settings")]
    [SerializeField] private Vector3 targetRotation = new(45, 0, 0);

    private void Update()
    {
        playerTagCanvas.transform.rotation = Quaternion.Euler(targetRotation);
    }

    public void SetPlayerTag(string playerTag)
    {
        playerNameTagText.SetText(playerTag);
    }
}
