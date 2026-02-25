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
        enabled = IsServer;
    }

    private void Start()
    {
        if (!IsServer) return;

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
        gameDuration -= Time.deltaTime;

        UpdateGameTime_ClientRpc(gameDuration);

        if (gameDuration <= 0)
        {
            gameDuration = 0;
            DisablePlayers();
            ScoreManager.Instance.ShowFinalScore();
            enabled = false;
        }
    }

    [ClientRpc]
    private void UpdateGameTime_ClientRpc(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        timerText.SetText(string.Format("{0:D2}:{1:D2}", minutes, seconds));

        if (remainingTime <= 0)
        {
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
        orderTimer += Time.deltaTime;

        if (orderTimer >= orderingRate)
        {
            orderTimer = 0;
            onCreateOrder.Invoke();
        }
    }
}