using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    private string heldItem;

    public EnvironmentManager environmentManager;
    public new Camera camera;

    private void Start()
    {
        if (environmentManager == null)
            environmentManager = FindFirstObjectByType<EnvironmentManager>();
        if (camera == null)
            camera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || camera == null)
            return;

        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        InteractAtScreenPosition(Mouse.current.position.ReadValue());
    }

    public void InteractAtScreenPosition(Vector2 screenPosition)
    {
        if (camera == null)
            return;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (HasTagInParents(hit.collider.transform, "Item"))
        {
            ItemData item = hit.collider.GetComponentInParent<ItemData>();
            if (item == null || string.IsNullOrWhiteSpace(item.itemName))
            {
                Debug.LogWarning("У предмета не настроен ItemData или itemName.", hit.collider);
                return;
            }

            heldItem = item.itemName;
            Debug.Log($"Взял предмет: {heldItem}");
            return;
        }

        if (HasTagInParents(hit.collider.transform, "Plant"))
        {
            if (string.IsNullOrEmpty(heldItem))
            {
                Debug.Log("Нужно взять предмет");
                return;
            }

            UseItem();
        }
    }

    private static bool HasTagInParents(Transform target, string tagName)
    {
        for (Transform current = target; current != null; current = current.parent)
        {
            if (current.CompareTag(tagName))
                return true;
        }

        return false;
    }

    private void UseItem()
    {
        if (environmentManager == null)
        {
            Debug.LogWarning("EnvironmentManager не найден.", this);
            heldItem = null;
            return;
        }

        switch (heldItem)
        {
            case "watering_can":
                environmentManager.AddHumidity(15f);
                Debug.Log("Полил растение");
                break;
            case "fertilizer":
                environmentManager.AddMinerals(20f);
                Debug.Log("Удобрил растение");
                break;
            case "uv_lamp":
                environmentManager.AddTemperature(10f);
                Debug.Log("Включил УФ-лампу");
                break;
            default:
                Debug.LogWarning($"Неизвестный предмет: {heldItem}", this);
                break;
        }

        heldItem = null;
    }

    public string GetHeldItem() => heldItem;
}
