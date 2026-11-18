using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PrototypeSmokeCheck
{
    private const string PendingKey = "SymbiosisPrototypeSmokeCheckPending";

    private static double finishAt;
    private static bool passed;
    private static EnvironmentManager environment;
    private static PlantGrowth plant;
    private static InteractionSystem interaction;
    private static DayCycleManager dayCycle;
    private static global::EventSystem stationEvents;

    static PrototypeSmokeCheck()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Symbiosis/Run Prototype Smoke Check")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            BeginChecks();
            return;
        }

        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false))
            return;

        SessionState.SetBool(PendingKey, false);
        EditorApplication.delayCall += BeginChecks;
    }

    private static void BeginChecks()
    {
        environment = UnityEngine.Object.FindFirstObjectByType<EnvironmentManager>();
        plant = UnityEngine.Object.FindFirstObjectByType<PlantGrowth>();
        interaction = UnityEngine.Object.FindFirstObjectByType<InteractionSystem>();
        dayCycle = UnityEngine.Object.FindFirstObjectByType<DayCycleManager>();
        stationEvents = UnityEngine.Object.FindFirstObjectByType<global::EventSystem>();

        passed = Check(environment != null, "EnvironmentManager exists")
            & Check(plant != null, "PlantGrowth exists")
            & Check(interaction != null, "InteractionSystem exists")
            & Check(dayCycle != null, "DayCycleManager exists")
            & Check(stationEvents != null, "Station EventSystem exists");

        if (!passed)
        {
            Finish(false);
            return;
        }

        passed &= Check(Mathf.Approximately(environment.temperature, 20f)
            && Mathf.Approximately(environment.humidity, 50f)
            && Mathf.Approximately(environment.co2, 30f)
            && Mathf.Approximately(environment.minerals, 40f), "initial environment values are correct");

        FieldInfo timerField = typeof(EnvironmentManager).GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo intervalField = typeof(EnvironmentManager).GetField("updateInterval", BindingFlags.Instance | BindingFlags.NonPublic);
        passed &= Check(timerField != null && intervalField != null
            && Mathf.Approximately((float)intervalField.GetValue(environment), 10f), "environment uses a 10-second update interval");
        if (timerField != null)
        {
            environment.temperature = 20f;
            environment.humidity = 50f;
            environment.co2 = 30f;
            timerField.SetValue(environment, 0f);
            environment.SendMessage("Update");
            passed &= Check(Mathf.Approximately(environment.temperature, 20f)
                && Mathf.Approximately(environment.humidity, 50f)
                && Mathf.Approximately(environment.co2, 30f), "environment does not decay before its interval");

            timerField.SetValue(environment, 1000f);
            environment.SendMessage("Update");
            passed &= Check(Mathf.Abs(environment.temperature - 19.95f) < 0.002f
                && Mathf.Abs(environment.humidity - 49.97f) < 0.002f
                && Mathf.Abs(environment.co2 - 30.02f) < 0.002f, "environment updates values once per interval");

            environment.temperature = 80f;
            environment.humidity = 50f;
            timerField.SetValue(environment, 1000f);
            environment.SendMessage("Update");
            passed &= Check(Mathf.Abs(environment.humidity - 49.94f) < 0.002f, "high temperature doubles humidity decay");

            float pausedTemperature = environment.temperature;
            float pausedHumidity = environment.humidity;
            float pausedCo2 = environment.co2;
            environment.StartNight();
            timerField.SetValue(environment, 1000f);
            environment.SendMessage("Update");
            passed &= Check(Mathf.Approximately(environment.temperature, pausedTemperature)
                && Mathf.Approximately(environment.humidity, pausedHumidity)
                && Mathf.Approximately(environment.co2, pausedCo2), "environment stops changing during night");
            environment.EndNight();
        }

        environment.temperature = 20f;
        environment.humidity = 50f;
        environment.co2 = 30f;
        environment.minerals = 40f;

        environment.AddHumidity(1000f);
        passed &= Check(Mathf.Approximately(environment.humidity, 100f), "humidity clamps at 100");
        environment.AddHumidity(-2000f);
        passed &= Check(Mathf.Approximately(environment.humidity, 0f), "humidity clamps at 0");
        environment.humidity = 50f;

        GameObject plantContainer = GameObject.Find("PlantContainer");
        Collider plantCollider = plantContainer != null ? plantContainer.GetComponentInChildren<Collider>() : null;
        Collider wateringCan = GameObject.Find("Item_WateringCan")?.GetComponent<Collider>();
        Collider fertilizer = GameObject.Find("Item_Fertilizer")?.GetComponent<Collider>();
        Collider uvLamp = GameObject.Find("Item_UVLamp")?.GetComponent<Collider>();
        Camera camera = Camera.main;
        passed &= Check(plantCollider != null && wateringCan != null && fertilizer != null && uvLamp != null && camera != null,
            "plant and tool colliders and main camera are wired");

        if (plantCollider == null || wateringCan == null || fertilizer == null || uvLamp == null || camera == null)
        {
            Finish(false);
            return;
        }

        interaction.InteractAtScreenPosition(camera.WorldToScreenPoint(wateringCan.bounds.center));
        passed &= Check(interaction.GetHeldItem() == "watering_can", "click ray picks up the watering can");
        interaction.InteractAtScreenPosition(camera.WorldToScreenPoint(plantCollider.bounds.center));
        passed &= Check(Mathf.Approximately(environment.humidity, 65f)
            && interaction.GetHeldItem() == null, "watering can adds 15 humidity and is consumed");

        interaction.InteractAtScreenPosition(camera.WorldToScreenPoint(fertilizer.bounds.center));
        interaction.InteractAtScreenPosition(camera.WorldToScreenPoint(plantCollider.bounds.center));
        passed &= Check(Mathf.Approximately(environment.minerals, 60f)
            && interaction.GetHeldItem() == null, "fertilizer adds 20 minerals and is consumed");

        environment.temperature = 20f;
        environment.humidity = 50f;
        environment.co2 = 30f;
        environment.minerals = 40f;
        interaction.InteractAtScreenPosition(camera.WorldToScreenPoint(uvLamp.bounds.center));
        interaction.InteractAtScreenPosition(camera.WorldToScreenPoint(plantCollider.bounds.center));
        passed &= Check(Mathf.Approximately(environment.temperature, 30f), "UV lamp adds 10 temperature");

        float savedChance = stationEvents.eventChance;
        stationEvents.eventChance = 1f;

        environment.temperature = 80f;
        stationEvents.possibleEvents = new[] { "lamp_break" };
        stationEvents.TriggerRandomEvent();
        passed &= Check(Mathf.Approximately(environment.temperature, 60f), "lamp break lowers temperature by 20");

        environment.humidity = 40f;
        stationEvents.possibleEvents = new[] { "pipe_burst" };
        stationEvents.TriggerRandomEvent();
        passed &= Check(Mathf.Approximately(environment.humidity, 70f), "pipe burst raises humidity by 30");

        environment.minerals = 40f;
        stationEvents.possibleEvents = new[] { "mineral_leak" };
        stationEvents.TriggerRandomEvent();
        passed &= Check(Mathf.Approximately(environment.minerals, 25f), "event applies its parameter effect");
        passed &= Check(environment.temperature >= 0f && environment.temperature <= 100f
            && environment.humidity >= 0f && environment.humidity <= 100f
            && environment.co2 >= 0f && environment.co2 <= 100f
            && environment.minerals >= 0f && environment.minerals <= 100f, "event keeps values in range");

        environment.temperature = 10f;
        environment.humidity = 10f;
        environment.co2 = 10f;
        environment.minerals = 10f;
        plant.CheckAndChangePhase();
        passed &= Check(plant.currentPhase == 0, "phase check selects sprout range");
        environment.temperature = 50f;
        environment.humidity = 50f;
        environment.co2 = 50f;
        environment.minerals = 50f;
        plant.CheckAndChangePhase();
        passed &= Check(plant.currentPhase == 1, "phase check selects healthy range");
        environment.temperature = 80f;
        environment.humidity = 80f;
        environment.co2 = 80f;
        environment.minerals = 80f;
        plant.CheckAndChangePhase();
        passed &= Check(plant.currentPhase == 2, "phase check selects aggressive range");

        environment.temperature = 30f;
        environment.humidity = 50f;
        environment.co2 = 30f;
        environment.minerals = 40f;
        plant.CheckAndChangePhase();
        stationEvents.eventChance = 0f;

        GameObject sleepButtonObject = GameObject.Find("Sleep Button");
        Button sleepButton = sleepButtonObject != null ? sleepButtonObject.GetComponent<Button>() : null;
        passed &= Check(sleepButton != null && sleepButton.onClick.GetPersistentEventCount() == 1,
            "sleep button has a saved callback");
        if (sleepButton == null)
        {
            stationEvents.eventChance = savedChance;
            Finish(false);
            return;
        }

        dayCycle.currentDay = 1;
        sleepButton.onClick.Invoke();
        finishAt = EditorApplication.timeSinceStartup + 2.5d;
        EditorApplication.update -= WaitForSleep;
        EditorApplication.update += WaitForSleep;
    }

    private static void WaitForSleep()
    {
        if (EditorApplication.timeSinceStartup < finishAt)
            return;

        EditorApplication.update -= WaitForSleep;
        passed &= Check(dayCycle.currentDay == 2, "sleep callback waits and advances to day 2");
        passed &= Check(plant.currentPhase == 1, "plant phase is checked at wake-up");

        stationEvents.eventChance = 0.3f;
        Finish(passed);
    }

    private static bool Check(bool condition, string description)
    {
        if (condition)
            Debug.Log($"[Symbiosis smoke check] PASS — {description}");
        else
            Debug.LogError($"[Symbiosis smoke check] FAIL — {description}");

        return condition;
    }

    private static void Finish(bool success)
    {
        Debug.Log(success
            ? "[Symbiosis smoke check] Completed: all checks passed."
            : "[Symbiosis smoke check] Completed with failures.");
        EditorApplication.isPlaying = false;
    }
}
