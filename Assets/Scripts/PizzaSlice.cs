using UnityEngine;

public class PizzaSlice : MonoBehaviour
{
    public string sliceType; // e.g., "Pepperoni", "Cheese"
    
    // Smoothly move to a parent slot
    public void MoveToSlot(Transform targetSlot)
    {
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(targetSlot));
    }

    private System.Collections.IEnumerator MoveRoutine(Transform target)
    {
        float duration = 0.3f;
        float elapsed = 0;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            yield return null;
        }

        transform.SetParent(target);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
