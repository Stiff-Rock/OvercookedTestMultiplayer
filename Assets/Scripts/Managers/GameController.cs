using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkObject))]
public class GameController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI timerText;
    private PlayerController[] players;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 600.0f;
    [SerializeField] private float orderingRate = 10.0f;
    [SerializeField] private float orderTimer;

    // Events
    public UnityEvent onCreateOrder;
    public UnityEvent onGameOver;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Debug.Log("OnNetworkSpawn");

        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");
        players = playerObjs
            .Select(p => p.GetComponent<PlayerController>())
            .Where(p => p != null)
            .ToArray();

        onCreateOrder.Invoke();
    }

    private void Update()
    {
        if (!IsServer) return;

        UpdateGameTime();
        OrderTick();
    }

    private void UpdateGameTime()
    {
        gameDuration = Mathf.Max(0, gameDuration - Time.deltaTime);

        int minutes = Mathf.FloorToInt(gameDuration / 60);
        int seconds = Mathf.FloorToInt(gameDuration % 60);

        timerText.SetText(string.Format("{0:D2}:{1:D2}", minutes, seconds));

        if (gameDuration <= 0)
        {
            DisablePlayers();
            onGameOver.Invoke();
            enabled = false;
        }
    }

    private void DisablePlayers()
    {
        foreach (PlayerController player in players)
        {
            player.ToggleActive(false);
        }
    }

    private void OrderTick()
    {
        Debug.Log("ORDER TICK");
        orderTimer += Time.deltaTime;

        if (orderTimer >= orderingRate)
        {
            orderTimer = 0;
            onCreateOrder.Invoke();
        }
    }
}