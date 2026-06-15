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
            return;
        }

        // Prefer the manager that actually has hold slots wired in the Inspector
        if (HasConfiguredSlots() && !Instance.HasConfiguredSlots())
        {
            Destroy(Instance.gameObject);
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private bool HasConfiguredSlots()
    {
        return holdSlots != null && holdSlots.Count > 0 && holdSlots[0] != null;
    }

    private void Start()
    {
        if (plateSpawner == null)
        {
            plateSpawner = FindFirstObjectByType<PlateSpawner>();
        }

        // Do not RefillAllSlots on Start anymore as we start in the Main Menu
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

    /// <summary>
    /// Destroys all plates currently stored in hold slots.
    /// </summary>
    public void ClearAllSlots()
    {
        foreach (var slot in holdSlots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                if (slot.CurrentPlate != null)
                {
                    Destroy(slot.CurrentPlate);
                }
                slot.ClearSlot();
            }
        }
    }
}
