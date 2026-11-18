using System.Collections;
using TMPro;
using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public EnvironmentManager environmentManager;
    public PlantGrowth plantGrowth;
    public int currentDay = 1;
    public int maxDays = 7;
    public TextMeshProUGUI dayText;

    private EventSystem eventSystem;
    private bool isSleeping;
    private bool gameEnded;

    private void Start()
    {
        if (environmentManager == null)
            environmentManager = FindFirstObjectByType<EnvironmentManager>();
        if (plantGrowth == null)
            plantGrowth = FindFirstObjectByType<PlantGrowth>();
        eventSystem = FindFirstObjectByType<EventSystem>();
        UpdateDayText();
    }

    public void Sleep()
    {
        if (isSleeping || gameEnded)
            return;

        if (environmentManager == null)
            environmentManager = FindFirstObjectByType<EnvironmentManager>();

        if (environmentManager == null)
        {
            Debug.LogError("Нельзя начать ночь: EnvironmentManager не найден.", this);
            return;
        }

        isSleeping = true;
        environmentManager.StartNight();
        StartCoroutine(SleepRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (plantGrowth != null)
            plantGrowth.CheckAndChangePhase();
        if (eventSystem != null)
            eventSystem.TriggerRandomEvent();

        currentDay = Mathf.Min(currentDay + 1, maxDays);
        UpdateDayText();
        environmentManager.EndNight();
        isSleeping = false;

        if (currentDay >= maxDays)
            EndGame();
    }

    private void UpdateDayText()
    {
        if (dayText != null)
            dayText.text = $"День: {currentDay}/{maxDays}";
    }

    public void EndGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        string finalPhase = plantGrowth != null ? plantGrowth.CurrentStateName : "неизвестна";
        Debug.Log($"Игра окончена. Финальная фаза растения: {finalPhase}", this);
    }
}
