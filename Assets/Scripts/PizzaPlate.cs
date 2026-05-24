using UnityEngine;
using System.Collections.Generic;

public class PizzaPlate : MonoBehaviour
{
    public string pizzaType; // Major type of pizza on this plate
    public float slotHeight = 0.15f; // Vertical offset for slices
    public float slotRadius = 0.4f;  // Distance from center
    public Transform[] sliceSlots; // 6 slots positioned in a circle
    
    private List<PizzaSlice> currentSlices = new List<PizzaSlice>();

    public int SliceCount => currentSlices.Count;
    public bool IsFull => currentSlices.Count >= 6;

    public List<PizzaSlice> GetAllSlices() => currentSlices;

    public void AddSlice(PizzaSlice slice)
    {
        if (IsFull) return;

        currentSlices.Add(slice);
        ReorganizeSlices();
    }

    public PizzaSlice RemoveLastSlice()
    {
        if (currentSlices.Count == 0) return null;

        PizzaSlice slice = currentSlices[currentSlices.Count - 1];
        currentSlices.RemoveAt(currentSlices.Count - 1);
        return slice;
    }

    public int GetCountByType(string type)
    {
        int count = 0;
        foreach (var s in currentSlices) if (s.sliceType == type) count++;
        return count;
    }

    public PizzaSlice RemoveSliceByType(string type)
    {
        for (int i = currentSlices.Count - 1; i >= 0; i--)
        {
            if (currentSlices[i].sliceType == type)
            {
                PizzaSlice slice = currentSlices[i];
                currentSlices.RemoveAt(i);
                ReorganizeSlices();
                return slice;
            }
        }
        return null;
    }

    private void ReorganizeSlices()
    {
        for (int i = 0; i < currentSlices.Count; i++)
        {
            currentSlices[i].MoveToSlot(sliceSlots[i]);
        }
    }

    public bool IsAllSameType()
    {
        if (currentSlices.Count == 0) return false;
        string firstType = currentSlices[0].sliceType;
        foreach (var s in currentSlices) if (s.sliceType != firstType) return false;
        return true;
    }

    public HashSet<string> GetTypesPresent()
    {
        HashSet<string> types = new HashSet<string>();
        foreach (var s in currentSlices) types.Add(s.sliceType);
        return types;
    }

    public void ClearPlate()
    {
        foreach (var s in currentSlices) if(s != null) Destroy(s.gameObject);
        currentSlices.Clear();
        // Notify Cell that it's free
        Cell cell = GetComponentInParent<Cell>();
        if (cell != null) cell.currentPlate = null;
        
        // Destroy the plate itself after a short delay or animation
        Destroy(gameObject, 0.5f);
    }

    // Auto-generate slots if not assigned (Manual setup helper)
    [ContextMenu("Generate Slots")]
    public void GenerateSlots()
    {
        // Clear existing slots if any
        foreach (var slot in sliceSlots) if(slot != null) DestroyImmediate(slot.gameObject);

        sliceSlots = new Transform[6];
        float radius = 0.4f;
        for (int i = 0; i < 6; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(this.transform);
            
            float angle = i * 60f * Mathf.Deg2Rad;
            slotObj.transform.localPosition = new Vector3(Mathf.Cos(angle) * slotRadius, slotHeight, Mathf.Sin(angle) * slotRadius);
            slotObj.transform.localRotation = Quaternion.Euler(0, -i * 60f, 0);
            
            sliceSlots[i] = slotObj.transform;
        }
    }
}
