using UnityEngine;
using System.Collections.Generic;

public class PooledObject : MonoBehaviour
{
    public string poolTag;

    [System.NonSerialized]
    public ParticleSystem[] cachedParticleSystems;

    private void Awake()
    {
        cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnDisable()
    {
        if (ObjectPooler.Instance != null && !string.IsNullOrEmpty(poolTag))
        {
            ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
        }
    }
}

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
            InitializePools();
            return;
        }

        if (HasConfiguredPools() && !Instance.HasConfiguredPools())
        {
            Destroy(Instance.gameObject);
            Instance = this;
            InitializePools();
            return;
        }

        Destroy(gameObject);
    }

    private bool HasConfiguredPools()
    {
        return pools != null && pools.Count > 0 && pools[0].prefab != null;
    }

    private void InitializePools()
    {
        _poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                continue;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, this.transform);
                
                PooledObject pooledObj = obj.GetComponent<PooledObject>();
                if (pooledObj == null) pooledObj = obj.AddComponent<PooledObject>();
                pooledObj.poolTag = pool.tag;

                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            _poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Spawns an object from the pool, activating it at the specified position and rotation.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!_poolDictionary.ContainsKey(tag))
        {
            return null;
        }

        Queue<GameObject> queue = _poolDictionary[tag];
        GameObject objToSpawn = null;

        // Try to find an inactive object in the queue
        int attempts = queue.Count;
        for (int i = 0; i < attempts; i++)
        {
            GameObject checkObj = queue.Dequeue();
            if (checkObj == null) continue;

            if (!checkObj.activeSelf)
            {
                objToSpawn = checkObj;
                break;
            }
            queue.Enqueue(checkObj);
        }

        // If no inactive object is available, dynamically grow the pool
        if (objToSpawn == null)
        {
            Pool poolConf = pools.Find(p => p.tag == tag);
            if (poolConf != null && poolConf.prefab != null)
            {
                objToSpawn = Instantiate(poolConf.prefab, this.transform);
                PooledObject pooled = objToSpawn.GetComponent<PooledObject>();
                if (pooled == null) pooled = objToSpawn.AddComponent<PooledObject>();
                pooled.poolTag = tag;
            }
            else
            {
                return null;
            }
        }

        objToSpawn.SetActive(true);
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;

        // Reset particle systems if present (using cached components to avoid GC Alloc)
        PooledObject pooledComp = objToSpawn.GetComponent<PooledObject>();
        if (pooledComp != null)
        {
            if (pooledComp.cachedParticleSystems == null)
            {
                pooledComp.cachedParticleSystems = objToSpawn.GetComponentsInChildren<ParticleSystem>(true);
            }
            foreach (var p in pooledComp.cachedParticleSystems)
            {
                if (p != null)
                {
                    p.Clear();
                    p.Play();
                }
            }
        }

        return objToSpawn;
    }

    /// <summary>
    /// Spawns an object from the pool, registering the pool dynamically with the provided prefab if it doesn't exist.
    /// </summary>
    public GameObject SpawnFromPool(string tag, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_poolDictionary.ContainsKey(tag))
        {
            RegisterDynamicPool(tag, prefab, 12);
        }
        return SpawnFromPool(tag, position, rotation);
    }

    public void RegisterDynamicPool(string tag, GameObject prefab, int size)
    {
        if (prefab == null) return;
        if (_poolDictionary.ContainsKey(tag)) return;

        Queue<GameObject> objectPool = new Queue<GameObject>();

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, this.transform);
            
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null) pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.poolTag = tag;

            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        _poolDictionary.Add(tag, objectPool);
        
        if (pools == null) pools = new List<Pool>();
        pools.Add(new Pool { tag = tag, prefab = prefab, size = size });
    }

    /// <summary>
    /// Returns an object to the pool, placing it back in the queue and reparenting it.
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (obj == null) return;

        if (!_poolDictionary.ContainsKey(tag))
        {
            return;
        }

        Queue<GameObject> queue = _poolDictionary[tag];
        if (!queue.Contains(obj))
        {
            queue.Enqueue(obj);
        }

        /*obj.transform.SetParent(this.transform);
        if (obj.activeSelf)
        {
            obj.SetActive(false);
        }*/
    }
}
