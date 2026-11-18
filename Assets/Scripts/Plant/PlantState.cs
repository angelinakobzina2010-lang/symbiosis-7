using UnityEngine;

[CreateAssetMenu(fileName = "PlantState", menuName = "Symbiosis/Plant State")]
public class PlantState : ScriptableObject
{
    public string stateName;
    [TextArea] public string description;

    [Header("Temperature")]
    public float minTemperature;
    public float maxTemperature = 100f;

    [Header("Humidity")]
    public float minHumidity;
    public float maxHumidity = 100f;

    [Header("CO2")]
    public float minCo2;
    public float maxCo2 = 100f;

    [Header("Minerals")]
    public float minMinerals;
    public float maxMinerals = 100f;

    public GameObject plantPrefab;

    public bool Matches(EnvironmentManager environment)
    {
        return environment != null
            && IsWithin(environment.temperature, minTemperature, maxTemperature)
            && IsWithin(environment.humidity, minHumidity, maxHumidity)
            && IsWithin(environment.co2, minCo2, maxCo2)
            && IsWithin(environment.minerals, minMinerals, maxMinerals);
    }

    private static bool IsWithin(float value, float minimum, float maximum)
    {
        return value >= minimum && value <= maximum;
    }
}
