using UnityEngine;
using System.Collections.Generic;

public class PizzaPlate : MonoBehaviour
{
    public string pizzaType; // Major type of pizza on this plate
    public float slotHeight = 0.15f; // Vertical offset for slices
    public float slotRadius = 0.4f;  // Distance from center
    public Transform[] sliceSlots; // 6 slots positioned in a circle
    
    private List<PizzaSlice> currentSlices = new List<PizzaSlice>();

    /// <summary>True while ClearPlate() is in progress. MergeManager must skip these plates.</summary>
    public bool IsBeingCleared { get; private set; } = false;

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
                // Reset the removed slice's slot cache — it no longer belongs to this plate
                slice.ResetTargetSlot();
                // Re-animate all remaining slices to their new index positions
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
            // Reset target cache so MoveToSlot never skips a slot due to index shift
            currentSlices[i].ResetTargetSlot();
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
        // Guard: prevent double-clear
        if (IsBeingCleared) return;
        IsBeingCleared = true;

        // ── Free the Cell IMMEDIATELY so FindBestMove() skips this spot ──
        Cell cell = GetComponentInParent<Cell>();
        if (cell != null) cell.currentPlate = null;

        // Spawn explosion and score popup from ObjectPooler
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.SpawnFromPool("PizzaExplosion", transform.position, Quaternion.identity);
            ObjectPooler.Instance.SpawnFromPool("ScorePopup", transform.position + Vector3.up * 0.6f, Quaternion.identity);
        }

        // Play sound with pitch shift combo feel
        if (ComboAudioPlayer.Instance != null)
        {
            ComboAudioPlayer.Instance.PlayExplosionWithCombo();
        }

        // Add score and gold to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(60); // 10 points per slice
            GameManager.Instance.AddGold(5);   // 5 gold per plate
        }

        foreach (var s in currentSlices) if (s != null) Destroy(s.gameObject);
        currentSlices.Clear();

        // Destroy the plate itself after a short visual delay
        Destroy(gameObject, 0.3f);
    }

    /// <summary>
    /// Plays a juice squash-and-stretch bounce effect (e.g. when snapping/landing on grid).
    /// </summary>
    public void PlayBounceAnimation()
    {
        StartCoroutine(BounceRoutine());
    }

    private System.Collections.IEnumerator BounceRoutine()
    {
        Vector3 initialScale = transform.localScale;
        
        // Phase 1: Squash (compress down, expand out)
        float elapsed = 0f;
        float duration = 0.07f;
        Vector3 targetScale = new Vector3(initialScale.x * 1.2f, initialScale.y * 0.6f, initialScale.z * 1.2f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            yield return null;
        }

        // Phase 2: Stretch (bounce high, contract sides)
        elapsed = 0f;
        duration = 0.1f;
        Vector3 previousScale = transform.localScale;
        targetScale = new Vector3(initialScale.x * 0.85f, initialScale.y * 1.25f, initialScale.z * 0.85f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(previousScale, targetScale, elapsed / duration);
            yield return null;
        }

        // Phase 3: Settle back to original scale
        elapsed = 0f;
        duration = 0.1f;
        previousScale = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(previousScale, initialScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = initialScale;
    }

    /// <summary>
    /// Plays a horizontal shake effect (e.g. when a player tries to drop another plate on top of this one).
    /// </summary>
    public void PlayShakeAnimation()
    {
        StartCoroutine(ShakeRoutine());
    }

    private System.Collections.IEnumerator ShakeRoutine()
    {
        Vector3 startPos = transform.position;
        float duration = 0.25f;
        float elapsed = 0f;
        float shakeAmount = 0.12f;
        float shakeSpeed = 45f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float shakeOffset = Mathf.Sin(elapsed * shakeSpeed) * shakeAmount * (1f - t);
            transform.position = new Vector3(startPos.x + shakeOffset, startPos.y, startPos.z);
            
            yield return null;
        }

        transform.position = startPos;
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
