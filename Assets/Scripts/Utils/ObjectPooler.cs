using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static ObjectPooler Instance { get; private set; }

    [Header("Pool Setup")]
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> _poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        _poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogWarning($"[ObjectPooler] Pool with tag {pool.tag} has a null prefab.");
                continue;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, this.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            _poolDictionary.Add(pool.tag, objectPool);
            Debug.Log($"[ObjectPooler] Created pool: {pool.tag} of size {pool.size}");
        }
    }

    /// <summary>
    /// Spawns an object from the pool, activating it at the specified position and rotation.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!_poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPooler] Pool with tag {tag} doesn't exist.");
            return null;
        }

        Queue<GameObject> queue = _poolDictionary[tag];
        if (queue.Count == 0)
        {
            Debug.LogWarning($"[ObjectPooler] Pool {tag} is empty!");
            return null;
        }

        // Get object from the front of the queue
        GameObject objToSpawn = queue.Dequeue();

        objToSpawn.SetActive(true);
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;

        // Re-enqueue it to the end of the queue so it can be reused later
        queue.Enqueue(objToSpawn);

        // Reset particle systems if present
        ParticleSystem[] particles = objToSpawn.GetComponentsInChildren<ParticleSystem>();
        foreach (var p in particles)
        {
            p.Clear();
            p.Play();
        }

        // If the object needs to auto-deactivate after a while, we can handle it via a component,
        // or a simple auto-deactivator script attached to the prefab.
        
        return objToSpawn;
    }
}
