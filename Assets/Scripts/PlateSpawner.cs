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
        // Auto-spawn initial plates on start
        SpawnRandomPlate();
    }

    private void Update()
    {
        // Press Space to quickly test generation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnRandomPlate();
        }
    }

    [ContextMenu("Spawn Test Plate")]
    public void SpawnRandomPlate()
    {
        if (spawnPoints.Length == 0 || platePrefab == null || pizzaSlicePrefabs.Length == 0)
        {
            Debug.LogWarning("Please assign prefabs and spawn points in PlateSpawner!");
            return;
        }

        // Choose a random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // Spawn the plate
        GameObject newPlate = Instantiate(platePrefab, spawnPoint.position, Quaternion.identity);
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
                    GameObject sliceObj = Instantiate(slicePrefab);
                    PizzaSlice sliceScript = sliceObj.GetComponent<PizzaSlice>();
                    if (sliceScript == null) sliceScript = sliceObj.AddComponent<PizzaSlice>();
                    
                    sliceScript.sliceType = type;
                    plateScript.AddSlice(sliceScript);
                }
                slicesRemaining -= countForThisType;
            }
        }
    }
}
