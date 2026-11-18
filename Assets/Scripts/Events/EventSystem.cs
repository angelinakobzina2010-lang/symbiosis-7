using UnityEngine;

public class EventSystem : MonoBehaviour
{
    public EnvironmentManager environmentManager;
    [Range(0f, 1f)] public float eventChance = 0.3f;
    public string[] possibleEvents = { "lamp_break", "pipe_burst", "mineral_leak" };

    public string LastEventMessage { get; private set; } = string.Empty;

    private void Start()
    {
        if (environmentManager == null)
            environmentManager = FindFirstObjectByType<EnvironmentManager>();
    }

    public void TriggerRandomEvent()
    {
        LastEventMessage = string.Empty;
        if (environmentManager == null || possibleEvents == null || possibleEvents.Length == 0)
            return;

        if (Random.value < eventChance)
            TriggerEvent(possibleEvents[Random.Range(0, possibleEvents.Length)]);
    }

    private void TriggerEvent(string eventName)
    {
        switch (eventName)
        {
            case "lamp_break":
                environmentManager.AddTemperature(-20f);
                LastEventMessage = "Событие: Поломка лампы. Температура упала.";
                break;
            case "pipe_burst":
                environmentManager.AddHumidity(30f);
                LastEventMessage = "Событие: Прорыв трубы. Влажность выросла.";
                break;
            case "mineral_leak":
                environmentManager.AddMinerals(-15f);
                LastEventMessage = "Событие: Утечка минералов. Уровень упал.";
                break;
            default:
                Debug.LogWarning($"Неизвестное событие: {eventName}", this);
                return;
        }

        Debug.Log(LastEventMessage, this);
    }
}
