using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SymbiosisPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/SymbiosisPrototype.unity";
    private const string StateFolder = "Assets/Data/PlantStates";
    private const string PlantPrefabFolder = "Assets/Prefabs/Plants";
    private const string ItemPrefabFolder = "Assets/Prefabs/Items";
    private const string MaterialFolder = "Assets/Materials/Prototype";

    [MenuItem("Symbiosis/Build 3D Playable Prototype")]
    public static void BuildPrototype()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning($"{ScenePath} уже существует. Сцена не перезаписана, чтобы сохранить возможные ручные изменения.");
            return;
        }

        TMP_Settings settings = FindTMPSettings();
        if (settings == null || TMP_Settings.defaultFontAsset == null)
        {
            ImportTMPResources();
            return;
        }

        CreateProjectAssetsAndScene();
    }

    [MenuItem("Symbiosis/Add Starter Plant to Prototype Scene")]
    public static void AddStarterPlantToSavedScene()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogWarning("Сначала создай сцену через Symbiosis/Build 3D Playable Prototype.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject container = GameObject.Find("PlantContainer");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PlantPrefabFolder}/Plant_Sprout.prefab");
        if (container == null || prefab == null)
        {
            Debug.LogError("Не найдены контейнер растения или префаб ростка.");
            return;
        }

        if (container.transform.childCount == 0)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Selection.activeGameObject = container;
        Debug.Log("Стартовый префаб Росток добавлен в PlantContainer.");
    }

    [MenuItem("Symbiosis/Repair Prototype Labels and Sleep Button")]
    public static void RepairPrototypeBindings()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Останови Play Mode перед сохранением сцены.");
            return;
        }

        if (!File.Exists(ScenePath))
        {
            Debug.LogWarning("Сцена прототипа ещё не создана.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name.StartsWith("Label - "))
                root.transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        GameObject uvLampObject = GameObject.Find("Item_UVLamp");
        GameObject uvLampLabel = GameObject.Find("Label - УФ-лампа");
        if (uvLampObject != null)
            uvLampObject.transform.position = new Vector3(0f, 0f, -2.8f);
        if (uvLampLabel != null)
            uvLampLabel.transform.position = new Vector3(0f, 1.9f, -2.8f);

        DayCycleManager dayCycleManager = UnityEngine.Object.FindFirstObjectByType<DayCycleManager>();
        GameObject buttonObject = GameObject.Find("Sleep Button");
        Button sleepButton = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (dayCycleManager != null && sleepButton != null && sleepButton.onClick.GetPersistentEventCount() == 0)
            UnityEventTools.AddPersistentListener(sleepButton.onClick, dayCycleManager.Sleep);

        if (sleepButton != null)
            EditorUtility.SetDirty(sleepButton);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Исправлены надписи инструментов и постоянная привязка кнопки «Спать».");
    }

    private static TMP_Settings FindTMPSettings()
    {
        string guid = AssetDatabase.FindAssets("t:TMP_Settings").FirstOrDefault();
        return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<TMP_Settings>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void ImportTMPResources()
    {
        UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
            .FirstOrDefault(item => item.name == "com.unity.ugui");
        if (package == null)
        {
            Debug.LogError("Не найден пакет Unity UI, нужный для TextMeshPro.");
            return;
        }

        string unityPackage = Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage");
        if (!File.Exists(unityPackage))
        {
            Debug.LogError($"Не найден пакет ресурсов TextMeshPro: {unityPackage}");
            return;
        }

        AssetDatabase.importPackageCompleted -= OnTMPResourcesImported;
        AssetDatabase.importPackageCompleted += OnTMPResourcesImported;
        AssetDatabase.ImportPackage(unityPackage, false);
        Debug.Log("Импортирую встроенные ресурсы TextMeshPro перед созданием HUD.");
    }

    private static void OnTMPResourcesImported(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnTMPResourcesImported;
        EditorApplication.delayCall += () =>
        {
            TMP_Settings settings = FindTMPSettings();
            if (settings == null || TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError("Ресурсы TextMeshPro импортированы, но стандартный шрифт не найден.");
                return;
            }

            CreateProjectAssetsAndScene();
        };
    }

    private static void CreateProjectAssetsAndScene()
    {
        EnsureFolder("Assets", "Data");
        EnsureFolder("Assets/Data", "PlantStates");
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Plants");
        EnsureFolder("Assets/Prefabs", "Items");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Prototype");

        EnsureTag("Item");
        EnsureTag("Plant");

        Material floorMaterial = CreateMaterial("StationFloor", new Color(0.16f, 0.22f, 0.22f));
        Material planterMaterial = CreateMaterial("Planter", new Color(0.29f, 0.22f, 0.16f));
        Material sproutMaterial = CreateMaterial("SproutGreen", new Color(0.29f, 0.78f, 0.38f));
        Material healthyMaterial = CreateMaterial("HealthyGreen", new Color(0.12f, 0.63f, 0.34f));
        Material aggressiveMaterial = CreateMaterial("AggressiveViolet", new Color(0.62f, 0.24f, 0.74f));
        Material waterMaterial = CreateMaterial("WateringCanBlue", new Color(0.18f, 0.56f, 0.82f));
        Material fertilizerMaterial = CreateMaterial("FertilizerGold", new Color(0.78f, 0.56f, 0.16f));
        Material lampMaterial = CreateMaterial("UvLampAmber", new Color(0.95f, 0.77f, 0.24f), 0.15f);

        GameObject sproutPrefab = CreatePlantPrefab("Plant_Sprout", sproutMaterial, PlantPrefabFolder, 0);
        GameObject healthyPrefab = CreatePlantPrefab("Plant_Healthy", healthyMaterial, PlantPrefabFolder, 1);
        GameObject aggressivePrefab = CreatePlantPrefab("Plant_Aggressive", aggressiveMaterial, PlantPrefabFolder, 2);
        GameObject wateringCanPrefab = CreateItemPrefab("Item_WateringCan", "watering_can", waterMaterial, ItemPrefabFolder, 0);
        GameObject fertilizerPrefab = CreateItemPrefab("Item_Fertilizer", "fertilizer", fertilizerMaterial, ItemPrefabFolder, 1);
        GameObject uvLampPrefab = CreateItemPrefab("Item_UVLamp", "uv_lamp", lampMaterial, ItemPrefabFolder, 2);

        PlantState sproutState = CreatePlantState("Sprout", "Росток", "Хрупкое начало роста.", 0f, 30f, sproutPrefab);
        PlantState healthyState = CreatePlantState("Healthy", "Здоровое", "Растение адаптировалось к станции.", 30f, 70f, healthyPrefab);
        PlantState aggressiveState = CreatePlantState("Aggressive", "Агрессивное", "Мутация вышла из-под контроля.", 70f, 100f, aggressivePrefab);
        AssetDatabase.SaveAssets();

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        BuildWorld(floorMaterial, planterMaterial, wateringCanPrefab, fertilizerPrefab, uvLampPrefab);
        Camera camera = BuildCameraAndLighting();

        GameObject environmentObject = new GameObject("EnvironmentManager");
        EnvironmentManager environmentManager = environmentObject.AddComponent<EnvironmentManager>();

        GameObject container = new GameObject("PlantContainer");
        container.transform.position = new Vector3(0f, 0.43f, 0f);
        GameObject plantObject = new GameObject("PlantGrowth");
        PlantGrowth plantGrowth = plantObject.AddComponent<PlantGrowth>();
        plantGrowth.environmentManager = environmentManager;
        plantGrowth.plantStates = new[] { sproutState, healthyState, aggressiveState };
        plantGrowth.currentPhase = 0;
        plantGrowth.plantContainer = container;
        GameObject starterPlant = (GameObject)PrefabUtility.InstantiatePrefab(sproutPrefab, container.transform);
        starterPlant.transform.localPosition = Vector3.zero;
        starterPlant.transform.localRotation = Quaternion.identity;
        starterPlant.transform.localScale = Vector3.one;

        GameObject eventsObject = new GameObject("StationEventSystem");
        global::EventSystem stationEvents = eventsObject.AddComponent<global::EventSystem>();
        stationEvents.environmentManager = environmentManager;

        GameObject interactionObject = new GameObject("InteractionSystem");
        InteractionSystem interaction = interactionObject.AddComponent<InteractionSystem>();
        interaction.environmentManager = environmentManager;
        interaction.camera = camera;

        DayCycleManager dayCycleManager = new GameObject("DayCycleManager").AddComponent<DayCycleManager>();
        dayCycleManager.environmentManager = environmentManager;
        dayCycleManager.plantGrowth = plantGrowth;

        TextMeshProUGUI dayText;
        TextMeshProUGUI temperatureText;
        TextMeshProUGUI humidityText;
        TextMeshProUGUI co2Text;
        TextMeshProUGUI mineralsText;
        TextMeshProUGUI heldItemText;
        Button sleepButton;
        BuildHud(out dayText, out temperatureText, out humidityText, out co2Text, out mineralsText, out heldItemText, out sleepButton);
        dayCycleManager.dayText = dayText;
        UnityEventTools.AddPersistentListener(sleepButton.onClick, dayCycleManager.Sleep);

        GameObject uiManagerObject = new GameObject("UIManager");
        UIManager uiManager = uiManagerObject.AddComponent<UIManager>();
        uiManager.environmentManager = environmentManager;
        uiManager.interactionSystem = interaction;
        uiManager.dayCycleManager = dayCycleManager;
        uiManager.dayText = dayText;
        uiManager.temperatureText = temperatureText;
        uiManager.humidityText = humidityText;
        uiManager.co2Text = co2Text;
        uiManager.mineralsText = mineralsText;
        uiManager.heldItemText = heldItemText;

        BuildUIEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        if (previousScene.IsValid() && previousScene != scene && !previousScene.isDirty && EditorSceneManager.sceneCount == 2)
            EditorSceneManager.CloseScene(previousScene, true);
        EditorSceneManager.SetActiveScene(scene);
        Selection.activeGameObject = plantObject;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Готово: создана сцена SymbiosisPrototype с системами игры, HUD и примитивными моделями-заглушками.");
    }

    private static void BuildWorld(Material floorMaterial, Material planterMaterial, GameObject wateringCan, GameObject fertilizer, GameObject uvLamp)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "StationFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2f, 1f, 1.5f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        planter.name = "PlantPot";
        planter.transform.position = new Vector3(0f, 0.24f, 0f);
        planter.transform.localScale = new Vector3(0.95f, 0.24f, 0.95f);
        planter.GetComponent<Renderer>().sharedMaterial = planterMaterial;

        PlacePrefab(wateringCan, new Vector3(-3.6f, 0f, 0.4f), new Vector3(-3.6f, 0f, 0.4f));
        PlacePrefab(fertilizer, new Vector3(3.6f, 0f, 0.4f), new Vector3(3.6f, 0f, 0.4f));
        PlacePrefab(uvLamp, new Vector3(0f, 0f, -2.8f), new Vector3(0f, 0f, -2.8f));

        CreateWorldLabel("Лейка", new Vector3(-3.6f, 1.8f, 0.4f));
        CreateWorldLabel("Удобрение", new Vector3(3.6f, 1.8f, 0.4f));
        CreateWorldLabel("УФ-лампа", new Vector3(0f, 1.9f, -2.8f));
    }

    private static Camera BuildCameraAndLighting()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 6.6f, -11f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.9f, 0f) - cameraObject.transform.position);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.1f, 0.12f);
        camera.fieldOfView = 48f;

        GameObject lightObject = new GameObject("Station Light", typeof(Light));
        lightObject.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;
        light.color = new Color(0.82f, 0.93f, 1f);
        RenderSettings.ambientLight = new Color(0.34f, 0.39f, 0.42f);
        return camera;
    }

    private static GameObject CreatePlantPrefab(string name, Material material, string folder, int phase)
    {
        GameObject root = new GameObject(name);
        root.tag = "Plant";
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.05f, 0f);
        collider.radius = 0.67f;
        collider.height = 2.3f;

        if (phase == 0)
        {
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.42f, 0f), new Vector3(0.09f, 0.43f, 0.09f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Leaf Left", new Vector3(-0.22f, 0.83f, 0f), new Vector3(0.42f, 0.18f, 0.28f), material, new Vector3(0f, 0f, -22f));
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Leaf Right", new Vector3(0.22f, 0.83f, 0f), new Vector3(0.42f, 0.18f, 0.28f), material, new Vector3(0f, 0f, 22f));
        }
        else if (phase == 1)
        {
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.84f, 0f), new Vector3(0.12f, 0.85f, 0.12f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Leaf Left", new Vector3(-0.39f, 1.06f, 0f), new Vector3(0.68f, 0.22f, 0.39f), material, new Vector3(0f, 0f, -24f));
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Leaf Right", new Vector3(0.38f, 1.25f, 0.02f), new Vector3(0.7f, 0.23f, 0.4f), material, new Vector3(0f, 0f, 24f));
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Leaf Back", new Vector3(0f, 1.5f, 0.25f), new Vector3(0.55f, 0.2f, 0.3f), material, new Vector3(0f, 20f, 0f));
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Bud", new Vector3(0f, 1.82f, 0f), new Vector3(0.34f, 0.38f, 0.34f), material);
        }
        else
        {
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.95f, 0f), new Vector3(0.15f, 0.96f, 0.15f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Crown", new Vector3(0f, 1.88f, 0f), new Vector3(0.62f, 0.78f, 0.62f), material);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(radians) * 0.62f, 1.12f + (i % 2) * 0.26f, Mathf.Sin(radians) * 0.62f);
                AddPrimitivePart(root.transform, PrimitiveType.Capsule, "Mutation Spike", position, new Vector3(0.27f, 0.57f, 0.27f), material, new Vector3(0f, 0f, angle * 0.2f));
            }
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "Eye", new Vector3(0f, 1.95f, -0.52f), new Vector3(0.2f, 0.2f, 0.11f), CreateMaterial("Eye", new Color(0.95f, 0.28f, 0.22f)));
        }

        return SavePrefabAndDestroy(root, folder, name);
    }

    private static GameObject CreateItemPrefab(string name, string itemName, Material material, string folder, int shape)
    {
        GameObject root = new GameObject(name);
        root.tag = "Item";
        ItemData data = root.AddComponent<ItemData>();
        data.itemName = itemName;
        BoxCollider collider = root.AddComponent<BoxCollider>();

        if (shape == 0)
        {
            collider.center = new Vector3(0f, 0.6f, 0f);
            collider.size = new Vector3(0.9f, 1.3f, 0.75f);
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Can body", new Vector3(0f, 0.54f, 0f), new Vector3(0.38f, 0.48f, 0.34f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Cube, "Handle", new Vector3(0.36f, 0.74f, 0f), new Vector3(0.13f, 0.48f, 0.14f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Spout", new Vector3(-0.37f, 0.86f, 0f), new Vector3(0.1f, 0.31f, 0.1f), material, new Vector3(0f, 0f, -55f));
        }
        else if (shape == 1)
        {
            collider.center = new Vector3(0f, 0.48f, 0f);
            collider.size = new Vector3(0.85f, 1.05f, 0.72f);
            AddPrimitivePart(root.transform, PrimitiveType.Cube, "Sack", new Vector3(0f, 0.46f, 0f), new Vector3(0.7f, 0.88f, 0.62f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Sack tie", new Vector3(0f, 0.96f, 0f), new Vector3(0.23f, 0.13f, 0.23f), material);
        }
        else
        {
            collider.center = new Vector3(0f, 0.77f, 0f);
            collider.size = new Vector3(0.7f, 1.65f, 0.7f);
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Lamp stand", new Vector3(0f, 0.68f, 0f), new Vector3(0.1f, 0.68f, 0.1f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Cylinder, "Lamp head", new Vector3(0f, 1.38f, 0f), new Vector3(0.37f, 0.2f, 0.37f), material);
            AddPrimitivePart(root.transform, PrimitiveType.Sphere, "UV bulb", new Vector3(0f, 1.17f, 0f), new Vector3(0.32f, 0.18f, 0.32f), material);
        }

        return SavePrefabAndDestroy(root, folder, name);
    }

    private static void AddPrimitivePart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        if (euler.HasValue)
            part.transform.localEulerAngles = euler.Value;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            Object.DestroyImmediate(partCollider);
    }

    private static GameObject SavePrefabAndDestroy(GameObject root, string folder, string name)
    {
        string path = $"{folder}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static PlantState CreatePlantState(string assetName, string stateName, string description, float min, float max, GameObject prefab)
    {
        string path = $"{StateFolder}/{assetName}.asset";
        PlantState state = AssetDatabase.LoadAssetAtPath<PlantState>(path);
        if (state == null)
        {
            state = ScriptableObject.CreateInstance<PlantState>();
            AssetDatabase.CreateAsset(state, path);
        }

        state.stateName = stateName;
        state.description = description;
        state.minTemperature = min;
        state.maxTemperature = max;
        state.minHumidity = min;
        state.maxHumidity = max;
        state.minCo2 = min;
        state.maxCo2 = max;
        state.minMinerals = min;
        state.maxMinerals = max;
        state.plantPrefab = prefab;
        EditorUtility.SetDirty(state);
        return state;
    }

    private static Material CreateMaterial(string name, Color color, float metallic = 0f)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void PlacePrefab(GameObject prefab, Vector3 position, Vector3 lookAt)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        Vector3 direction = lookAt - position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            instance.transform.rotation = Quaternion.LookRotation(direction);
    }

    private static void CreateWorldLabel(string label, Vector3 position)
    {
        GameObject textObject = new GameObject($"Label - {label}", typeof(TextMesh));
        textObject.transform.position = position;
        textObject.transform.rotation = Quaternion.LookRotation(Vector3.back);
        textObject.transform.localScale = new Vector3(-1f, 1f, 1f);
        TextMesh text = textObject.GetComponent<TextMesh>();
        text.text = label;
        text.fontSize = 64;
        text.characterSize = 0.055f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = new Color(0.9f, 0.95f, 0.9f);
    }

    private static void BuildHud(out TextMeshProUGUI day, out TextMeshProUGUI temperature, out TextMeshProUGUI humidity,
        out TextMeshProUGUI co2, out TextMeshProUGUI minerals, out TextMeshProUGUI heldItem, out Button sleepButton)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Environment Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(28f, -28f);
        panelRect.sizeDelta = new Vector2(455f, 322f);
        panel.GetComponent<Image>().color = new Color(0.035f, 0.075f, 0.08f, 0.88f);

        day = CreateHudText(panel.transform, "Day Text", "День: 1/7", 24f, 20f, 46f, new Color(0.57f, 0.93f, 0.76f), FontStyles.Bold);
        temperature = CreateHudText(panel.transform, "Temperature Text", "Температура: 20", 24f, 20f, 98f, Color.white);
        humidity = CreateHudText(panel.transform, "Humidity Text", "Влажность: 50", 24f, 20f, 142f, Color.white);
        co2 = CreateHudText(panel.transform, "CO2 Text", "CO2: 30", 24f, 20f, 186f, Color.white);
        minerals = CreateHudText(panel.transform, "Minerals Text", "Минералы: 40", 24f, 20f, 230f, Color.white);
        heldItem = CreateHudText(panel.transform, "Held Item Text", "В руке: ничего", 24f, 20f, 274f, new Color(0.85f, 0.91f, 0.86f));

        GameObject hintObject = new GameObject("Controls Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(canvas.transform, false);
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(0f, 0f);
        hintRect.pivot = new Vector2(0f, 0f);
        hintRect.anchoredPosition = new Vector2(32f, 32f);
        hintRect.sizeDelta = new Vector2(740f, 64f);
        TextMeshProUGUI hint = hintObject.GetComponent<TextMeshProUGUI>();
        hint.font = TMP_Settings.defaultFontAsset;
        hint.fontSize = 20f;
        hint.color = Color.white;
        hint.text = "Кликни инструмент, затем растение. Ночью фаза проверяется при пробуждении.";
        hint.raycastTarget = false;

        GameObject buttonObject = new GameObject("Sleep Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-34f, 30f);
        buttonRect.sizeDelta = new Vector2(270f, 74f);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.53f, 0.38f, 0.98f);
        sleepButton = buttonObject.GetComponent<Button>();
        ColorBlock colors = sleepButton.colors;
        colors.highlightedColor = new Color(0.29f, 0.68f, 0.49f, 1f);
        colors.pressedColor = new Color(0.12f, 0.35f, 0.24f, 1f);
        sleepButton.colors = colors;

        GameObject buttonTextObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI buttonText = buttonTextObject.GetComponent<TextMeshProUGUI>();
        buttonText.font = TMP_Settings.defaultFontAsset;
        buttonText.fontSize = 23f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        buttonText.text = "СПАТЬ · 2 СЕК.";
        buttonText.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateHudText(Transform parent, string name, string value, float size, float x, float y, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(410f, 40f);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static void BuildUIEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private static void EnsureTag(string tag)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0)
            return;

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = $"{parent}/{folder}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] updated = new[] { new EditorBuildSettingsScene(scenePath, true) }
            .Concat(existing.Where(scene => scene.enabled && scene.path != scenePath))
            .ToArray();
        EditorBuildSettings.scenes = updated;
    }
}
