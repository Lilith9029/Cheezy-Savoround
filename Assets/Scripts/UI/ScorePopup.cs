using UnityEngine;
using TMPro;
using System.Collections;

public class ScorePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro tmpText;

    public void Show(int points, Vector3 worldPosition)
    {
        transform.position = worldPosition;
        tmpText.text = $"+{points}";
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        StopAllCoroutines();
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float duration = 1.2f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.forward * 1.5f;
        Color startColor = tmpText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, endPos, t);

            if (t > 0.5f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                tmpText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}