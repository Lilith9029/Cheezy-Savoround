using UnityEngine;

public class PizzaSlice : MonoBehaviour
{
    public string sliceType; // e.g., "Pepperoni", "Cheese"
    
    public float arcHeight = 0.8f; // Height of the Bezier arc

    private Coroutine _moveCoroutine;
    
    // Track which slot we are currently moving toward to avoid duplicate calls
    private Transform _targetSlot;

    /// <summary>
    /// Resets the target slot cache so MoveToSlot will animate even if pointing to the same Transform.
    /// Must be called whenever the slice is re-indexed within a plate.
    /// </summary>
    public void ResetTargetSlot()
    {
        _targetSlot = null;
    }

    /// <summary>
    /// Smoothly moves this slice to the target slot along a Bezier arc.
    /// Safe to call even if a previous move is in progress.
    /// </summary>
    public void MoveToSlot(Transform targetSlot)
    {
        // Skip if we are already heading to exactly this slot
        if (targetSlot == _targetSlot) return;
        _targetSlot = targetSlot;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveRoutine(targetSlot));
    }

    private System.Collections.IEnumerator MoveRoutine(Transform target)
    {
        float duration = 0.35f;
        float elapsed = 0;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            // Safety: if target was destroyed mid-flight, abort
            if (target == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smooth step for ease-in-ease-out transition
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);
            
            Vector3 endPos = target.position;
            Vector3 controlPoint = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * arcHeight;
            
            // Quadratic Bezier: B(t) = (1-t)^2*P0 + 2(1-t)t*P1 + t^2*P2
            float u = 1f - tSmooth;
            Vector3 currentPos = u * u * startPos + 2f * u * tSmooth * controlPoint + tSmooth * tSmooth * endPos;
            
            transform.position = currentPos;
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, tSmooth);
            yield return null;
        }

        if (target == null) yield break;

        transform.SetParent(target);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _moveCoroutine = null;
        _targetSlot = target;
    }
}
