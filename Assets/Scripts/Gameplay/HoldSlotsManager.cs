using UnityEngine;
using System.Collections.Generic;

public class HoldSlotsManager : MonoBehaviour
{
    public static HoldSlotsManager Instance { get; private set; }

    [Header("Hold Slots")]
    [Tooltip("Assign the 3 HoldSlot game objects here.")]
    public List<HoldSlot> holdSlots = new List<HoldSlot>();
    
    [Header("Spawner Reference")]
    public PlateSpawner plateSpawner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (plateSpawner == null)
        {
            plateSpawner = FindFirstObjectByType<PlateSpawner>();
        }

        // Initially fill all slots
        RefillAllSlots();
    }

    /// <summary>
    /// Check if all slots are empty. If so, triggers a refill.
    /// Called when a plate is successfully dropped on the Grid.
    /// </summary>
    public void CheckAndRefillSlots()
    {
        bool allEmpty = true;
        foreach (var slot in holdSlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                allEmpty = false;
                break;
            }
        }

        if (allEmpty)
        {
            Debug.Log("[HoldSlots] All slots empty! Refilling...");
            RefillAllSlots();
        }
    }

    /// <summary>
    /// Refills all empty hold slots.
    /// </summary>
    public void RefillAllSlots()
    {
        if (plateSpawner == null)
        {
            Debug.LogError("[HoldSlotsManager] PlateSpawner reference is missing!");
            return;
        }

        foreach (var slot in holdSlots)
        {
            if (slot != null && slot.IsEmpty)
            {
                GameObject newPlate = plateSpawner.SpawnPlate();
                if (newPlate != null)
                {
                    slot.AssignPlate(newPlate);
                }
            }
        }
    }
}
