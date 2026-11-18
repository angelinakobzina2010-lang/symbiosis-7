using UnityEngine;

public class PlantGrowth : MonoBehaviour
{
    public EnvironmentManager environmentManager;
    public PlantState[] plantStates = new PlantState[3];
    public int currentPhase;
    public GameObject plantContainer;

    private GameObject activePlant;

    public string CurrentStateName
    {
        get
        {
            if (plantStates == null || currentPhase < 0 || currentPhase >= plantStates.Length)
                return "неизвестна";

            return plantStates[currentPhase] != null ? plantStates[currentPhase].stateName : "неизвестна";
        }
    }

    private void Start()
    {
        if (environmentManager == null)
            environmentManager = FindFirstObjectByType<EnvironmentManager>();

        if (plantContainer != null && plantContainer.transform.childCount > 0)
            activePlant = plantContainer.transform.GetChild(0).gameObject;
        else
            ActivatePhase(currentPhase);
    }

    public void CheckAndChangePhase()
    {
        if (environmentManager == null || plantStates == null)
            return;

        for (int i = 0; i < plantStates.Length; i++)
        {
            PlantState state = plantStates[i];
            if (state == null || !state.Matches(environmentManager))
                continue;

            if (i != currentPhase && ActivatePhase(i))
                Debug.Log($"Смена фазы на: {state.stateName}", this);
            return;
        }
    }

    private bool ActivatePhase(int phaseIndex)
    {
        if (plantStates == null || phaseIndex < 0 || phaseIndex >= plantStates.Length || plantStates[phaseIndex] == null)
        {
            Debug.LogWarning($"Не настроена фаза растения с индексом {phaseIndex}.", this);
            return false;
        }

        PlantState state = plantStates[phaseIndex];
        if (state.plantPrefab == null || plantContainer == null)
        {
            Debug.LogWarning($"Для фазы «{state.stateName}» не назначен префаб растения или контейнер.", this);
            return false;
        }

        if (activePlant != null)
            Destroy(activePlant);

        activePlant = Instantiate(state.plantPrefab, plantContainer.transform);
        activePlant.transform.localPosition = Vector3.zero;
        activePlant.transform.localRotation = Quaternion.identity;
        activePlant.transform.localScale = Vector3.one;
        currentPhase = phaseIndex;
        return true;
    }
}
