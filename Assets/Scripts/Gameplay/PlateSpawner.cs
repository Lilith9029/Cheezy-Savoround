using UnityEngine;
using System.Collections.Generic;

public class PlateSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject platePrefab;
    public GameObject[] pizzaSlicePrefabs; // Assign Pizza 1 to Pizza 6 prefabs here
    public Transform[] spawnPoints; // Where new plates appear

    private void Start()
    {
        // Spawning will be managed by HoldSlotsManager on start
    }

    private void Update()
    {
        // Press Space to quickly test generation (independent spawn)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnRandomPlate();
        }
    }

    /// <summary>
    /// Spawns a plate at a random spawn point (mainly for test/fallback).
    /// </summary>
    [ContextMenu("Spawn Test Plate")]
    public void SpawnRandomPlate()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject newPlate = SpawnPlate();
        if (newPlate != null)
        {
            newPlate.transform.position = spawnPoint.position;
        }
    }

    /// <summary>
    /// Spawns a new plate with randomized pizza slices and returns it.
    /// </summary>
    public GameObject SpawnPlate()
    {
        if (platePrefab == null || pizzaSlicePrefabs == null || pizzaSlicePrefabs.Length == 0)
        {
            return null;
        }

        GameObject newPlate = Instantiate(platePrefab, Vector3.zero, Quaternion.identity);
        if (newPlate.GetComponent<PlateSkin>() == null)
        {
            newPlate.AddComponent<PlateSkin>();
        }
        PizzaPlate plateScript = newPlate.GetComponent<PizzaPlate>();

        if (plateScript != null)
        {
            plateScript.GenerateSlots();
            
            // Spawn 1-5 total slices, mixed with 1-3 types
            int totalSlicesToSpawn = Random.Range(1, 6); 
            int numberOfTypes = (totalSlicesToSpawn > 1) ? Random.Range(1, 4) : 1;
            
            int slicesRemaining = totalSlicesToSpawn;

            for (int t = 0; t < numberOfTypes; t++)
            {
                if (slicesRemaining <= 0) break;

                int randomPizzaIndex = Random.Range(0, pizzaSlicePrefabs.Length);
                GameObject slicePrefab = pizzaSlicePrefabs[randomPizzaIndex];
                string type = (randomPizzaIndex + 1).ToString();

                int countForThisType = (t == numberOfTypes - 1) ? slicesRemaining : Random.Range(1, slicesRemaining);

                for (int i = 0; i < countForThisType; i++)
                {
                    string slicePoolTag = "PizzaSlice_" + type;
                    GameObject sliceObj;
                    if (ObjectPooler.Instance != null)
                    {
                        sliceObj = ObjectPooler.Instance.SpawnFromPool(slicePoolTag, slicePrefab, Vector3.zero, Quaternion.identity);
                    }
                    else
                    {
                        sliceObj = Instantiate(slicePrefab);
                    }

                    PizzaSlice sliceScript = sliceObj.GetComponent<PizzaSlice>();
                    if (sliceScript == null) sliceScript = sliceObj.AddComponent<PizzaSlice>();
                    
                    sliceScript.sliceType = type;
                    plateScript.AddSlice(sliceScript);
                }
                slicesRemaining -= countForThisType;
            }
        }

        return newPlate;
    }
}
