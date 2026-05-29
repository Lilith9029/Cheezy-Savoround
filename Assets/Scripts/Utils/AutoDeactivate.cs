using UnityEngine;

public class AutoDeactivate : MonoBehaviour
{
    [Tooltip("Time in seconds before the object is deactivated.")]
    [SerializeField] private float lifetime = 1.5f;
    
    private float _timer;

    private void OnEnable()
    {
        _timer = lifetime;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
