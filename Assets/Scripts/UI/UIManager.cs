using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public EnvironmentManager environmentManager;
    public TextMeshProUGUI temperatureText;
    public TextMeshProUGUI humidityText;
    public TextMeshProUGUI co2Text;
    public TextMeshProUGUI mineralsText;
    public TextMeshProUGUI heldItemText;
    public InteractionSystem interactionSystem;
    public DayCycleManager dayCycleManager;
    public TextMeshProUGUI dayText;

    private void Start()
    {
        if (environmentManager == null)
            environmentManager = FindFirstObjectByType<EnvironmentManager>();
        if (interactionSystem == null)
            interactionSystem = FindFirstObjectByType<InteractionSystem>();
        if (dayCycleManager == null)
            dayCycleManager = FindFirstObjectByType<DayCycleManager>();
    }

    private void Update()
    {
        if (environmentManager != null)
        {
            if (temperatureText != null) temperatureText.text = $"Температура: {Mathf.RoundToInt(environmentManager.temperature)}";
            if (humidityText != null) humidityText.text = $"Влажность: {Mathf.RoundToInt(environmentManager.humidity)}";
            if (co2Text != null) co2Text.text = $"CO2: {Mathf.RoundToInt(environmentManager.co2)}";
            if (mineralsText != null) mineralsText.text = $"Минералы: {Mathf.RoundToInt(environmentManager.minerals)}";
        }

        if (heldItemText != null)
        {
            string item = interactionSystem != null ? interactionSystem.GetHeldItem() : null;
            heldItemText.text = $"В руке: {GetDisplayName(item)}";
        }

        if (dayText != null && dayCycleManager != null)
            dayText.text = $"День: {dayCycleManager.currentDay}/{dayCycleManager.maxDays}";
    }

    private static string GetDisplayName(string item)
    {
        switch (item)
        {
            case "watering_can": return "лейка";
            case "fertilizer": return "удобрение";
            case "uv_lamp": return "УФ-лампа";
            default: return "ничего";
        }
    }
}
