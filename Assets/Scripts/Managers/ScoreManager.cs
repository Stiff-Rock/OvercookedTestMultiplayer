using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    // GameOverPanel
    [SerializeField] private TextMeshProUGUI deliveredOrdersText;
    [SerializeField] private TextMeshProUGUI failedOrdersText;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Values")]
    [SerializeField] private int currentScore;
    [SerializeField] private int deliveredOrdersCount;
    [SerializeField] private int failedOrdersCount;

    [Header("Score Settings")]
    [SerializeField] private int ingredientScoreValue = 50;
    [SerializeField] private int timeScoreValue = 5;
    [SerializeField] private int expireScorePenalty = 100;
    [SerializeField] private float pointsAnimationDuration = 1.5f;

    public void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(gameObject);
    }

    public void RewardScore(KitchenOrder order)
    {
        int orderScore = order.Recipe.GetTotalIngredients() * ingredientScoreValue;
        int lifespanScoreBonus = (int)order.Lifespan * timeScoreValue;
        int totalScore = orderScore + lifespanScoreBonus;
        UpdateScoreValue(totalScore);
        ++deliveredOrdersCount;
    }

    public void PenalizeScore()
    {
        UpdateScoreValue(-expireScorePenalty);
        ++failedOrdersCount;
    }

    private void UpdateScoreValue(int change)
    {
        currentScore += change;
        UpdateScoreText_ClientRpc(currentScore);
    }

    [ClientRpc]
    private void UpdateScoreText_ClientRpc(int newScore)
    {
        scoreText.SetText($"{newScore}");
    }

    public void ShowFinalScore()
    {
        ShowFinalScore_ClientRpc(
            deliveredOrdersCount,
            failedOrdersCount,
            currentScore
        );
    }

    [ClientRpc]
    private void ShowFinalScore_ClientRpc(int deliveredOrdersCount, int failedOrdersCount, int currentScore)
    {
        StartCoroutine(StartPointsAnimationSequence(
            deliveredOrdersCount,
            failedOrdersCount,
            currentScore
        ));
    }

    private IEnumerator StartPointsAnimationSequence(int dOC, int fOC, int cS)
    {
        yield return StartCoroutine(AnimatePoints(deliveredOrdersText, dOC));
        yield return StartCoroutine(AnimatePoints(failedOrdersText, fOC));
        yield return StartCoroutine(AnimatePoints(finalScoreText, cS));
    }

    private IEnumerator AnimatePoints(TextMeshProUGUI textField, int targetValue)
    {
        float elapsed = 0f;
        int.TryParse(textField.text, out int startValue);

        if (startValue == targetValue)
        {
            textField.SetText(targetValue.ToString());
            yield break;
        }

        while (elapsed < pointsAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float percentage = elapsed / pointsAnimationDuration;

            int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, percentage));
            textField.SetText(currentValue.ToString());

            yield return null;
        }

        textField.SetText(targetValue.ToString());
    }
}