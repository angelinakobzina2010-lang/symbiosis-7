using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Range(0f, 100f)] public float temperature = 20f;
    [Range(0f, 100f)] public float humidity = 50f;
    [Range(0f, 100f)] public float co2 = 30f;
    [Range(0f, 100f)] public float minerals = 40f;

    [SerializeField] private float temperatureDecayRate = 0.05f;
    [SerializeField] private float humidityDecayRate = 0.03f;
    [SerializeField] private float co2GrowthRate = 0.02f;
    [SerializeField] private float updateInterval = 10f;

    private float timer;
    private bool isNight;

    private void Update()
    {
        if (isNight)
            return;

        timer += Time.deltaTime;
        if (timer < updateInterval)
            return;

        timer = 0f;
        float humidityMultiplier = temperature > 70f ? 2f : 1f;
        temperature = Mathf.Clamp(temperature - temperatureDecayRate, 0f, 100f);
        humidity = Mathf.Clamp(humidity - humidityDecayRate * humidityMultiplier, 0f, 100f);
        co2 = Mathf.Clamp(co2 + co2GrowthRate, 0f, 100f);
    }

    public void AddTemperature(float amount) => temperature = Mathf.Clamp(temperature + amount, 0f, 100f);
    public void AddHumidity(float amount) => humidity = Mathf.Clamp(humidity + amount, 0f, 100f);
    public void AddCo2(float amount) => co2 = Mathf.Clamp(co2 + amount, 0f, 100f);
    public void AddMinerals(float amount) => minerals = Mathf.Clamp(minerals + amount, 0f, 100f);

    public void StartNight()
    {
        isNight = true;
        timer = 0f;
    }

    public void EndNight()
    {
        isNight = false;
        timer = 0f;
    }
}
