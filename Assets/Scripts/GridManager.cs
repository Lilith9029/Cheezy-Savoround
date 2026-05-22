using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private string configFileName = "GridConfig";
    
    private void Start()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        // Load JSON config from Resources
        TextAsset configAsset = Resources.Load<TextAsset>(configFileName);
        if (configAsset == null)
        {
            Debug.LogError($"Could not find configuration file: {configFileName} in Resources");
            return;
        }

        GridData data = JsonUtility.FromJson<GridData>(configAsset.text);
        if (data == null)
        {
            Debug.LogError("Failed to parse GridData from JSON.");
            return;
        }

        GenerateGrid(data);
    }

    private void GenerateGrid(GridData data)
    {
        // Center the grid
        Vector3 offset = new Vector3(
            (data.width - 1) * data.spacing / 2f,
            0,
            (data.height - 1) * data.spacing / 2f
        );

        for (int x = 0; x < data.width; x++)
        {
            for (int z = 0; z < data.height; z++)
            {
                Vector3 position = new Vector3(x * data.spacing, 0, z * data.spacing) - offset;
                
                // Create a cube for each cell
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = position;
                cube.transform.SetParent(this.transform);
                cube.name = $"Cell_{x}_{z}";
                
                // Slightly scale down the cube to see a gap if needed
                cube.transform.localScale = Vector3.one * 0.9f;
            }
        }
    }
}
